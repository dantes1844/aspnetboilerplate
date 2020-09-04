using Abp;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace AbpAspNetCoreDemo.Laobai
{
    public class GlobalRegisteredActionFilterAttribute : ActionFilterAttribute
    {
        private readonly PositionOptions _settings;

        public GlobalRegisteredActionFilterAttribute(IOptions<PositionOptions> options)
        {
            _settings = options.Value;
            Order = 1;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(GlobalRegisteredActionFilterAttribute)},{nameof(OnActionExecuting)},{_settings.Name}");
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(GlobalRegisteredActionFilterAttribute)},{nameof(OnActionExecuted)},{_settings.Name}");
            base.OnActionExecuted(context);
        }
    }
    public class SurroundClassActionFilterAttribute : ActionFilterAttribute
    {
        private readonly string _name;

        public SurroundClassActionFilterAttribute(string name)
        {
            _name = name;
            Order = 1;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(SurroundClassActionFilterAttribute)},{nameof(OnActionExecuting)},{_name}");
            context.HttpContext.Response.Headers.Add(_name,
                new[] { _name });
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(SurroundClassActionFilterAttribute)},{nameof(OnActionExecuted)},{_name}");
            base.OnActionExecuted(context);
        }
    }

    public class LaobaiResultActionFilter : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(LaobaiResultActionFilter)},{nameof(OnResultExecuting)}");
            //context.Cancel = true;//设置成true则短路当前操作，后面的返回都是空值
            base.OnResultExecuting(context);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(LaobaiResultActionFilter)},{nameof(OnResultExecuted)}");
            base.OnResultExecuted(context);
        }
    }
}
