using System.Linq;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.MultiTenancy;
using Abp.AspNetCore.Mvc.Auditing;
using Abp.AspNetCore.Runtime.Session;
using Abp.AspNetCore.Security.AntiForgery;
using Abp.AspNetCore.Webhook;
using Abp.Auditing;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Session;
using Abp.Web;
using Abp.Web.Security.AntiForgery;
using Abp.Webhooks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;

namespace Abp.AspNetCore
{
    [DependsOn(typeof(AbpWebCommonModule))]
    public class AbpAspNetCoreModule : AbpModule
    {
        public override void PreInitialize()
        {
            IocManager.AddConventionalRegistrar(new AbpAspNetCoreConventionalRegistrar());

            IocManager.Register<IAbpAspNetCoreConfiguration, AbpAspNetCoreConfiguration>();

            Configuration.ReplaceService<IPrincipalAccessor, AspNetCorePrincipalAccessor>(DependencyLifeStyle.Transient);
            Configuration.ReplaceService<IAbpAntiForgeryManager, AbpAspNetCoreAntiForgeryManager>(DependencyLifeStyle.Transient);
            Configuration.ReplaceService<IClientInfoProvider, HttpContextClientInfoProvider>(DependencyLifeStyle.Transient);
            Configuration.ReplaceService<IWebhookSender, AspNetCoreWebhookSender>(DependencyLifeStyle.Transient);

            Configuration.Modules.AbpAspNetCore().FormBodyBindingIgnoredTypes.Add(typeof(IFormFile));

            Configuration.MultiTenancy.Resolvers.Add<DomainTenantResolveContributor>();
            Configuration.MultiTenancy.Resolvers.Add<HttpHeaderTenantResolveContributor>();
            Configuration.MultiTenancy.Resolvers.Add<HttpCookieTenantResolveContributor>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetCoreModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            AddApplicationParts();
            ConfigureAntiforgery();
        }

        /// <summary>
        /// 这个方法就是将服务类转成控制器的
        /// </summary>
        private void AddApplicationParts()
        {
            var configuration = IocManager.Resolve<AbpAspNetCoreConfiguration>();
            var partManager = IocManager.Resolve<ApplicationPartManager>();//微软的库，动态的添加视图和控制器
            var moduleManager = IocManager.Resolve<IAbpModuleManager>();

            //当前程序集加入的目的是什么？ todo 2020年4月30日 16:08:10
            partManager.AddApplicationPartsIfNotAddedBefore(typeof(AbpAspNetCoreModule).Assembly);

            //这里就是将服务类转换为控制器的过程
            var controllerAssemblies = configuration.ControllerAssemblySettings.Select(s => s.Assembly).Distinct();
            foreach (var controllerAssembly in controllerAssemblies)
            {
                partManager.AddApplicationPartsIfNotAddedBefore(controllerAssembly);
            }

            //插件管理：将插件的内容也增加到视图、控制器中
            var plugInAssemblies = moduleManager.Modules.Where(m => m.IsLoadedAsPlugIn).Select(m => m.Assembly).Distinct();
            foreach (var plugInAssembly in plugInAssemblies)
            {
                partManager.AddApplicationPartsIfNotAddedBefore(plugInAssembly);
            }
        }

        private void ConfigureAntiforgery()
        {
            IocManager.Using<IOptions<AntiforgeryOptions>>(optionsAccessor =>
            {
                optionsAccessor.Value.HeaderName = Configuration.Modules.AbpWebCommon().AntiForgery.TokenHeaderName;
            });
        }
    }
}