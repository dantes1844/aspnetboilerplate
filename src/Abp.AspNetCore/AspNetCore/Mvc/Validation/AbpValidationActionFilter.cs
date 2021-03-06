﻿using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Aspects;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.Dependency;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Abp.AspNetCore.Mvc.Validation
{
    /// <summary>
    /// https://stackoverflow.com/questions/42582758/asp-net-core-middleware-vs-filters
    /// filter和middleware的区别
    /// </summary>
    public class AbpValidationActionFilter : IAsyncActionFilter, ITransientDependency
    {
        private readonly IIocResolver _iocResolver;
        private readonly IAbpAspNetCoreConfiguration _configuration;

        public AbpValidationActionFilter(IIocResolver iocResolver, IAbpAspNetCoreConfiguration configuration)
        {
            _iocResolver = iocResolver;
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //当前方法或者控制器都没有定义校验特性，直接执行方法。
            if (!_configuration.IsValidationEnabledForControllers || !context.ActionDescriptor.IsControllerAction())
            {
                await next();
                return;
            }

            //首先给当前方法增加拦截器记录，表示已经经过AbpCrossCuttingConcerns.Validation处理了。
            using (AbpCrossCuttingConcerns.Applying(context.Controller, AbpCrossCuttingConcerns.Validation))
            {
                using (var validator = _iocResolver.ResolveAsDisposable<MvcActionInvocationValidator>())
                {
                    //然后真正的调用Validate方法
                    validator.Object.Initialize(context);
                    validator.Object.Validate();
                }

                await next();
            }
        }
    }
}
