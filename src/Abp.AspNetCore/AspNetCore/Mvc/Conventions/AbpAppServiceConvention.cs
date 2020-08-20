using System;
using System.Collections.Generic;
using System.Diagnostics;
using Abp.Application.Services;
using Abp.AspNetCore.Configuration;
using Abp.Extensions;
using Castle.Windsor.MsDependencyInjection;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Abp.Collections.Extensions;
using Abp.Web.Api.ProxyScripting.Generators;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Abp.AspNetCore.Mvc.Conventions
{
    /// <summary>
    /// 配置全局的api路径，这个配置方法Apply是由.net core本身来调用的，所以在这里看不到引用次数。
    /// 参考连接：https://www.cnblogs.com/savorboard/p/dontnet-IApplicationModelConvention.html
    /// </summary>
    public class AbpAppServiceConvention : IApplicationModelConvention
    {
        private readonly Lazy<AbpAspNetCoreConfiguration> _configuration;

        public AbpAppServiceConvention(IServiceCollection services)
        {
            _configuration = new Lazy<AbpAspNetCoreConfiguration>(() =>
            {
                return services
                    .GetSingletonService<AbpBootstrapper>()
                    .IocManager
                    .Resolve<AbpAspNetCoreConfiguration>();
            }, true);
        }

        /// <summary>
        /// 核心实现
        /// </summary>
        /// <param name="application"></param>
        public void Apply(ApplicationModel application)
        {
            //这里已经拿到了所有的控制器信息，包括web层和service层的。要查看此前是怎么将service转成控制器的
            AbpDebug.WriteLine($"全部的控制器信息：{application.Controllers.Select(c => c.ControllerName).JoinAsString(" \r\n ")}");
            foreach (var controller in application.Controllers)
            {
                var tempName = controller.ControllerName;
                var type = controller.ControllerType.AsType();
                var configuration = GetControllerSettingOrNull(type);

                //判断当前控制器是否由服务直接生成，是的话移除后缀。并生成相应的路径配置
                if (typeof(IApplicationService).GetTypeInfo().IsAssignableFrom(type))
                {
                    //移除类名的后缀："AppService", "ApplicationService" 
                    controller.ControllerName = controller.ControllerName.RemovePostFix(ApplicationService.CommonPostfixes);
                    AbpDebug.WriteLine($"controller.ControllerName={controller.ControllerName},configuration.ModuleName:{configuration?.ModuleName}");
                    configuration?.ControllerModelConfigurer(controller);

                    //把service生成的api模块配置成area，本身的控制器视图页已经有了area的路由配置，无需处理
                    ConfigureArea(controller, configuration);
                    //配置web api
                    ConfigureRemoteService(controller, configuration);
                }
                else
                {
                    //如果是由控制器类实例得来的，判断是否有RemoteService标签。如果有，也要生成相应的 路径。
                    var remoteServiceAtt = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(type.GetTypeInfo());
                    if (remoteServiceAtt != null && remoteServiceAtt.IsEnabledFor(type))
                    {
                        ConfigureRemoteService(controller, configuration);
                    }
                }
            }
        }

        private void ConfigureArea(ControllerModel controller, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            if (configuration == null)
            {
                return;
            }

            if (controller.RouteValues.ContainsKey("area"))
            {
                return;
            }

            controller.RouteValues["area"] = configuration.ModuleName;
        }

        private void ConfigureRemoteService(ControllerModel controller, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            ConfigureApiExplorer(controller);
            ConfigureSelector(controller, configuration);
            ConfigureParameters(controller);
        }

        private void ConfigureParameters(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                foreach (var prm in action.Parameters)
                {
                    if (prm.BindingInfo != null)
                    {
                        continue;
                    }

                    if (!TypeHelper.IsPrimitiveExtendedIncludingNullable(prm.ParameterInfo.ParameterType))
                    {
                        if (CanUseFormBodyBinding(action, prm))
                        {
                            prm.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
                        }
                    }
                }
            }
        }

        private bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
        {
            if (_configuration.Value.FormBodyBindingIgnoredTypes.Any(t => t.IsAssignableFrom(parameter.ParameterInfo.ParameterType)))
            {
                return false;
            }

            foreach (var selector in action.Selectors)
            {
                if (selector.ActionConstraints == null)
                {
                    continue;
                }

                foreach (var actionConstraint in selector.ActionConstraints)
                {
                    var httpMethodActionConstraint = actionConstraint as HttpMethodActionConstraint;
                    if (httpMethodActionConstraint == null)
                    {
                        continue;
                    }

                    if (httpMethodActionConstraint.HttpMethods.All(hm => hm.IsIn("GET", "DELETE", "TRACE", "HEAD")))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ConfigureApiExplorer(ControllerModel controller)
        {
            if (controller.ApiExplorer.GroupName.IsNullOrEmpty())
            {
                controller.ApiExplorer.GroupName = controller.ControllerName;
            }
            if (controller.ApiExplorer.IsVisible == null)
            {
                var controllerType = controller.ControllerType.AsType();
                var remoteServiceAtt = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(controllerType.GetTypeInfo());
                if (remoteServiceAtt != null)
                {
                    controller.ApiExplorer.IsVisible =
                        remoteServiceAtt.IsEnabledFor(controllerType) &&
                        remoteServiceAtt.IsMetadataEnabledFor(controllerType);
                }
                else
                {
                    controller.ApiExplorer.IsVisible = true;
                }
            }

            foreach (var action in controller.Actions)
            {
                ConfigureApiExplorer(action);
            }
        }

        private void ConfigureApiExplorer(ActionModel action)
        {
            //如果action上设置了有ApiExplorerSettings，那么就使用原生的api生成，没有则使用abp模式
            if (action.ApiExplorer.IsVisible == null)
            {
                var remoteServiceAtt = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(action.ActionMethod);
                if (remoteServiceAtt != null)
                {
                    action.ApiExplorer.IsVisible =
                        remoteServiceAtt.IsEnabledFor(action.ActionMethod) &&
                        remoteServiceAtt.IsMetadataEnabledFor(action.ActionMethod);
                }
            }
        }

        private void ConfigureSelector(ControllerModel controller, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            RemoveEmptySelectors(controller.Selectors);

            if (controller.Selectors.Any(selector => selector.AttributeRouteModel != null))
            {
                return;
            }

            var moduleName = GetModuleNameOrDefault(controller.ControllerType.AsType());

            foreach (var action in controller.Actions)
            {
                ConfigureSelector(moduleName, controller.ControllerName, action, configuration);
            }
        }

        private void ConfigureSelector(string moduleName, string controllerName, ActionModel action, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            RemoveEmptySelectors(action.Selectors);

            var remoteServiceAtt = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(action.ActionMethod);
            //这里是说如果没打了这个标签，但是标签不适用于方法那么就不处理这个action了
            if (remoteServiceAtt != null && !remoteServiceAtt.IsEnabledFor(action.ActionMethod))
            {
                return;
            }

            if (!action.Selectors.Any())
            {
                //当前action一个选择器都没有的时候，使用默认的选择器
                AddAbpServiceSelector(moduleName, controllerName, action, configuration);
            }
            else
            {
                //否则遍历所有的选择器，将没有路由属性的选择器设置默认的选择器
                NormalizeSelectorRoutes(moduleName, controllerName, action);
            }
        }

        private void AddAbpServiceSelector(string moduleName, string controllerName, ActionModel action, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            var abpServiceSelectorModel = new SelectorModel
            {
                AttributeRouteModel = CreateAbpServiceAttributeRouteModel(moduleName, controllerName, action)
            };

            //计算当前api地址的动作约束
            //如果没有设置，则使用post，
            //否则通过方法名称计算：根据方法名开头是否包含诸如Get，Post等字符串来确定
            var verb = configuration?.UseConventionalHttpVerbs == true
                           ? ProxyScriptingHelper.GetConventionalVerbForMethodName(action.ActionName)
                           : ProxyScriptingHelper.DefaultHttpVerb;

            //添加动作约束
            abpServiceSelectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));

            //给action的选择器增加api路径选择器
            action.Selectors.Add(abpServiceSelectorModel);
        }

        private static void NormalizeSelectorRoutes(string moduleName, string controllerName, ActionModel action)
        {
            foreach (var selector in action.Selectors)
            {
                if (selector.AttributeRouteModel == null)
                {
                    selector.AttributeRouteModel = CreateAbpServiceAttributeRouteModel(
                        moduleName,
                        controllerName,
                        action
                    );
                }
            }
        }

        private string GetModuleNameOrDefault(Type controllerType)
        {
            return GetControllerSettingOrNull(controllerType)?.ModuleName ??
                   AbpControllerAssemblySetting.DefaultServiceModuleName;
        }

        [CanBeNull]
        private AbpControllerAssemblySetting GetControllerSettingOrNull(Type controllerType)
        {
            var settings = _configuration.Value.ControllerAssemblySettings.GetSettings(controllerType);
            return settings.FirstOrDefault(setting => setting.TypePredicate(controllerType));
        }

        /// <summary>
        /// 这里就是创建统一api路径的地方了
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="controllerName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private static AttributeRouteModel CreateAbpServiceAttributeRouteModel(string moduleName, string controllerName, ActionModel action)
        {
            return new AttributeRouteModel(
                new RouteAttribute(
                    $"api/services/{moduleName}/{controllerName}/{action.ActionName}"
                )
            );
        }

        private static void RemoveEmptySelectors(IList<SelectorModel> selectors)
        {
            selectors
                .Where(IsEmptySelector)
                .ToList()
                .ForEach(s => selectors.Remove(s));
        }

        private static bool IsEmptySelector(SelectorModel selector)
        {
            return selector.AttributeRouteModel == null && selector.ActionConstraints.IsNullOrEmpty();
        }
    }
}