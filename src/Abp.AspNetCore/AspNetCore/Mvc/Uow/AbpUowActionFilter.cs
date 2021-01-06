using System.Threading.Tasks;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Abp.AspNetCore.Mvc.Uow
{
    public class AbpUowActionFilter : IAsyncActionFilter, ITransientDependency
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IAbpAspNetCoreConfiguration _aspnetCoreConfiguration;
        private readonly IUnitOfWorkDefaultOptions _unitOfWorkDefaultOptions;

        public AbpUowActionFilter(
            IUnitOfWorkManager unitOfWorkManager,
            IAbpAspNetCoreConfiguration aspnetCoreConfiguration,
            IUnitOfWorkDefaultOptions unitOfWorkDefaultOptions)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _aspnetCoreConfiguration = aspnetCoreConfiguration;
            _unitOfWorkDefaultOptions = unitOfWorkDefaultOptions;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //不是控制器方法，直接调用该方法并返回，无需工作单元
            if (!context.ActionDescriptor.IsControllerAction())
            {
                await next();
                return;
            }
            //获取方法或者所属类定义的工作单元，如果没有，给默认的工作单元实例
            var unitOfWorkAttr = _unitOfWorkDefaultOptions
                .GetUnitOfWorkAttributeOrNull(context.ActionDescriptor.GetMethodInfo()) ??
                _aspnetCoreConfiguration.DefaultUnitOfWorkAttribute;


            //未启用UOW的直接调用action，否则用UOW实例将action包裹
            if (unitOfWorkAttr.IsDisabled)
            {
                await next();
                return;
            }

            //自动将UOW特性解析成工作单元模块
            using (var uow = _unitOfWorkManager.Begin(unitOfWorkAttr.CreateOptions()))
            {
                //2020年8月25日 疑问是：这里只有一个action，它要工作单元有什么用处？？
                var result = await next();
                if (result.Exception == null || result.ExceptionHandled)
                {
                    await uow.CompleteAsync();
                }
            }
        }
    }
}
