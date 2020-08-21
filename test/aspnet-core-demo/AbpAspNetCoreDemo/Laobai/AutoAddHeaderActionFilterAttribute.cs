using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(GlobalRegisteredActionFilterAttribute)},{_settings.Name}");
            context.HttpContext.Response.Headers.Add(_settings.Title,
                new string[] { _settings.Name });
            base.OnResultExecuting(context);
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

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            AbpDebug.WriteLine($"执行了{nameof(SurroundClassActionFilterAttribute)},{_name}");
            context.HttpContext.Response.Headers.Add(_name,
                new string[] { _name });
            base.OnResultExecuting(context);
        }
    }
}
