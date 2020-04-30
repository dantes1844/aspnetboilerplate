using System;
using System.Collections.Generic;
using System.Reflection;
using Abp.AspNetCore.Mvc.Results.Caching;
using Abp.Domain.Uow;
using Abp.Web.Models;
using Microsoft.AspNetCore.Routing;

namespace Abp.AspNetCore.Configuration
{
    public class AbpAspNetCoreConfiguration : IAbpAspNetCoreConfiguration
    {
        public WrapResultAttribute DefaultWrapResultAttribute { get; }

        [Obsolete]
        public IClientCacheAttribute DefaultClientCacheAttribute { get; set; }

        public UnitOfWorkAttribute DefaultUnitOfWorkAttribute { get; }

        public List<Type> FormBodyBindingIgnoredTypes { get; }

        public ControllerAssemblySettingList ControllerAssemblySettings { get; }

        public bool IsValidationEnabledForControllers { get; set; }

        public bool IsAuditingEnabled { get; set; }

        public bool SetNoCacheForAjaxResponses { get; set; }

        public bool UseMvcDateTimeFormatForAppServices { get; set; }

        public List<Action<IEndpointRouteBuilder>> EndpointConfiguration { get; }


        public AbpAspNetCoreConfiguration()
        {
            DefaultWrapResultAttribute = new WrapResultAttribute();
            DefaultClientCacheAttribute = new NoClientCacheAttribute(false);
            DefaultUnitOfWorkAttribute = new UnitOfWorkAttribute();
            ControllerAssemblySettings = new ControllerAssemblySettingList();
            FormBodyBindingIgnoredTypes = new List<Type>();
            EndpointConfiguration = new List<Action<IEndpointRouteBuilder>>();
            IsValidationEnabledForControllers = true;
            SetNoCacheForAjaxResponses = true;
            IsAuditingEnabled = true;
            UseMvcDateTimeFormatForAppServices = false;
        }
       
        public AbpControllerAssemblySettingBuilder CreateControllersForAppServices(
            Assembly assembly,
            string moduleName = AbpControllerAssemblySetting.DefaultServiceModuleName,
            bool useConventionalHttpVerbs = true)
        {
            var setting = new AbpControllerAssemblySetting(moduleName, assembly, useConventionalHttpVerbs);
            ControllerAssemblySettings.Add(setting);//这一行是这个方法的主要目的
            return new AbpControllerAssemblySettingBuilder(setting);//这个返回值貌似没有用上？todo 2020年4月30日 15:57:34
        }
    }
}