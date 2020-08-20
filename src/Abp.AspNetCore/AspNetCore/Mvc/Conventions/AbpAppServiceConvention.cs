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
    /// ����ȫ�ֵ�api·����������÷���Apply����.net core���������õģ����������￴�������ô�����
    /// �ο����ӣ�https://www.cnblogs.com/savorboard/p/dontnet-IApplicationModelConvention.html
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
        /// ����ʵ��
        /// </summary>
        /// <param name="application"></param>
        public void Apply(ApplicationModel application)
        {
            //�����Ѿ��õ������еĿ�������Ϣ������web���service��ġ�Ҫ�鿴��ǰ����ô��serviceת�ɿ�������
            AbpDebug.WriteLine($"ȫ���Ŀ�������Ϣ��{application.Controllers.Select(c => c.ControllerName).JoinAsString(" \r\n ")}");
            foreach (var controller in application.Controllers)
            {
                var tempName = controller.ControllerName;
                var type = controller.ControllerType.AsType();
                var configuration = GetControllerSettingOrNull(type);

                //�жϵ�ǰ�������Ƿ��ɷ���ֱ�����ɣ��ǵĻ��Ƴ���׺����������Ӧ��·������
                if (typeof(IApplicationService).GetTypeInfo().IsAssignableFrom(type))
                {
                    //�Ƴ������ĺ�׺��"AppService", "ApplicationService" 
                    controller.ControllerName = controller.ControllerName.RemovePostFix(ApplicationService.CommonPostfixes);
                    AbpDebug.WriteLine($"controller.ControllerName={controller.ControllerName},configuration.ModuleName:{configuration?.ModuleName}");
                    configuration?.ControllerModelConfigurer(controller);

                    //��service���ɵ�apiģ�����ó�area������Ŀ�������ͼҳ�Ѿ�����area��·�����ã����账��
                    ConfigureArea(controller, configuration);
                    //����web api
                    ConfigureRemoteService(controller, configuration);
                }
                else
                {
                    //������ɿ�������ʵ�������ģ��ж��Ƿ���RemoteService��ǩ������У�ҲҪ������Ӧ�� ·����
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
            //���action����������ApiExplorerSettings����ô��ʹ��ԭ����api���ɣ�û����ʹ��abpģʽ
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
            //������˵���û���������ǩ�����Ǳ�ǩ�������ڷ�����ô�Ͳ��������action��
            if (remoteServiceAtt != null && !remoteServiceAtt.IsEnabledFor(action.ActionMethod))
            {
                return;
            }

            if (!action.Selectors.Any())
            {
                //��ǰactionһ��ѡ������û�е�ʱ��ʹ��Ĭ�ϵ�ѡ����
                AddAbpServiceSelector(moduleName, controllerName, action, configuration);
            }
            else
            {
                //����������е�ѡ��������û��·�����Ե�ѡ��������Ĭ�ϵ�ѡ����
                NormalizeSelectorRoutes(moduleName, controllerName, action);
            }
        }

        private void AddAbpServiceSelector(string moduleName, string controllerName, ActionModel action, [CanBeNull] AbpControllerAssemblySetting configuration)
        {
            var abpServiceSelectorModel = new SelectorModel
            {
                AttributeRouteModel = CreateAbpServiceAttributeRouteModel(moduleName, controllerName, action)
            };

            //���㵱ǰapi��ַ�Ķ���Լ��
            //���û�����ã���ʹ��post��
            //����ͨ���������Ƽ��㣺���ݷ�������ͷ�Ƿ��������Get��Post���ַ�����ȷ��
            var verb = configuration?.UseConventionalHttpVerbs == true
                           ? ProxyScriptingHelper.GetConventionalVerbForMethodName(action.ActionName)
                           : ProxyScriptingHelper.DefaultHttpVerb;

            //��Ӷ���Լ��
            abpServiceSelectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));

            //��action��ѡ��������api·��ѡ����
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
        /// ������Ǵ���ͳһapi·���ĵط���
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