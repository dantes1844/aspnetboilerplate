using System.Web;
using System.Web.Mvc;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Web.Mvc.Configuration;
using Abp.Web.Mvc.Extensions;

namespace Abp.Web.Mvc.Uow
{
    public class AbpMvcUowFilter: IActionFilter, ITransientDependency
    {
        public const string UowHttpContextKey = "__AbpUnitOfWork";

        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IAbpMvcConfiguration _mvcConfiguration;
        private readonly IUnitOfWorkDefaultOptions _unitOfWorkDefaultOptions;

        public AbpMvcUowFilter(
            IUnitOfWorkManager unitOfWorkManager,
            IAbpMvcConfiguration mvcConfiguration, 
            IUnitOfWorkDefaultOptions unitOfWorkDefaultOptions)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _mvcConfiguration = mvcConfiguration;
            _unitOfWorkDefaultOptions = unitOfWorkDefaultOptions;
        }

        /// <summary>
        /// action执行之前，增加工作单元包裹
        /// </summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // 非视图方法不增加工作单元
            if (filterContext.IsChildAction)
            {
                return;
            }
            //获取方法的相关信息
            var methodInfo = filterContext.ActionDescriptor.GetMethodInfoOrNull();
            if (methodInfo == null)
            {
                return;
            }
            //判断该方法（或者其所属类）是否定义了UnitOfWorkAttribute
            var unitOfWorkAttr =
                _unitOfWorkDefaultOptions.GetUnitOfWorkAttributeOrNull(methodInfo) ??
                _mvcConfiguration.DefaultUnitOfWorkAttribute;
            //该方法禁用了UnitOfWork，直接返回
            if (unitOfWorkAttr.IsDisabled)
            {
                return;
            }
            //启用了UnitOfWork，则将请求上下文及该方法包裹在一个工作单元中
            SetCurrentUow(
                filterContext.HttpContext,
                _unitOfWorkManager.Begin(unitOfWorkAttr.CreateOptions())
            );
        }
        /// <summary>
        /// 方法执行完成之后，提交工作单元
        /// </summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.IsChildAction)
            {
                return;
            }
            //当前方法没有工作单元标记，直接返回
            var uow = GetCurrentUow(filterContext.HttpContext);
            if (uow == null)
            {
                return;
            }

            try
            {
                //没有产生异常，提交工作单元
                if (filterContext.Exception == null)
                {
                    uow.Complete();
                }
            }
            finally
            {
                //释放工作单元，清空请求上下文中的工作单元字典
                uow.Dispose();
                SetCurrentUow(filterContext.HttpContext, null);
            }
        }

        private static IUnitOfWorkCompleteHandle GetCurrentUow(HttpContextBase httpContext)
        {
            //从请求上下文中获取当前工作单元：保证每个请求只有一个工作单元？
            return httpContext.Items[UowHttpContextKey] as IUnitOfWorkCompleteHandle;
        }

        private static void SetCurrentUow(HttpContextBase httpContext, IUnitOfWorkCompleteHandle uow)
        {
            httpContext.Items[UowHttpContextKey] = uow;
        }
    }
}
