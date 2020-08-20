using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Abp.AspNetCore.Security
{
    public class AbpSecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public AbpSecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 中间件必须包含一个Invoke方法，且第一个参数必须是HttpContext类型
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {
            /*
             X-Content-Type-Options header tells the browser to not try and “guess” what a mimetype of a resource might be,
             and to just take what mimetype the server has returned as fact.

            nosniff阻止请求，如果请求的类型是

            “ style”，而 MIME 类型不是“ text/css”，或者
            纠错
            “ script”，而 MIME 类型不是 JavaScript MIME 类型。
             */
            AddHeaderIfNotExists(httpContext, "X-Content-Type-Options", "nosniff");

            /*
             阻止XSS攻击，当前的参数是指：启用，当遇到攻击时，阻止浏览器显示页面。
             X-XSS-Protection is a feature of Internet Explorer, Chrome and Safari that stops pages
             from loading when they detect reflected cross-site scripting (XSS) attacks
             https://cloud.tencent.com/developer/section/1190033
             */
            AddHeaderIfNotExists(httpContext, "X-XSS-Protection", "1; mode=block");

            /*
             * The X-Frame-Options HTTP response header can be used to indicate
             * whether or not a browser should be allowed to render a page in a <frame>, <iframe> or <object>.
             * SAMEORIGIN makes it being displayed in a frame on the same origin as the page itself.
             * The spec leaves it up to browser vendors to decide whether this option applies to the top level, the parent, or the whole chain
             */
            AddHeaderIfNotExists(httpContext, "X-Frame-Options", "SAMEORIGIN");//允许同源的地址出现在iframe中

            await _next.Invoke(httpContext);
        }

        private static void AddHeaderIfNotExists(HttpContext context, string key, string value)
        {
            if (!context.Response.Headers.ContainsKey(key))
            {
                context.Response.Headers.Add(key, value);
            }
        }
    }
}
