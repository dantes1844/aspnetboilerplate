using Abp.AspNetCore.Mvc.Auditing;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetCore.Mvc.Conventions;
using Abp.AspNetCore.Mvc.ExceptionHandling;
using Abp.AspNetCore.Mvc.ModelBinding;
using Abp.AspNetCore.Mvc.Results;
using Abp.AspNetCore.Mvc.Uow;
using Abp.AspNetCore.Mvc.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.AspNetCore.Mvc
{
    internal static class AbpMvcOptionsExtensions
    {
        public static void AddAbp(this MvcOptions options, IServiceCollection services)
        {
            AddConventions(options, services);
            AddActionFilters(options);
            AddPageFilters(options);
            AddModelBinders(options);
        }

        private static void AddConventions(MvcOptions options, IServiceCollection services)
        {
            //服务转成接口的约定
            options.Conventions.Add(new AbpAppServiceConvention(services));
        }

        private static void AddActionFilters(MvcOptions options)
        {
            options.Filters.AddService(typeof(AbpAuthorizationFilter));
            options.Filters.AddService(typeof(AbpAuditActionFilter));
            options.Filters.AddService(typeof(AbpValidationActionFilter));
            options.Filters.AddService(typeof(AbpUowActionFilter));//全局添加UOW过滤器
            options.Filters.AddService(typeof(AbpExceptionFilter));
            options.Filters.AddService(typeof(AbpResultFilter));
        }

        private static void AddPageFilters(MvcOptions options)
        {
            options.Filters.AddService(typeof(AbpUowPageFilter));
            options.Filters.AddService(typeof(AbpAuditPageFilter));
            options.Filters.AddService(typeof(AbpResultPageFilter));
            options.Filters.AddService(typeof(AbpExceptionPageFilter));
        }

        private static void AddModelBinders(MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new AbpDateTimeModelBinderProvider());
        }
    }
}