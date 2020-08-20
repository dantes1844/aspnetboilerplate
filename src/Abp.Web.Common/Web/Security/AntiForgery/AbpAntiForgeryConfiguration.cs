using System.Diagnostics;

namespace Abp.Web.Security.AntiForgery
{
    public class AbpAntiForgeryConfiguration : IAbpAntiForgeryConfiguration
    {
        public string TokenCookieName { get; set; }

        public string TokenHeaderName { get; set; }

        public string AuthorizationCookieName { get; set; }

        public string AuthorizationCookieApplicationScheme { get; set; }
        
        public AbpAntiForgeryConfiguration()
        {
            AbpDebug.WriteLine($"执行了{nameof(AbpAntiForgeryConfiguration)}的构造函数");
            TokenCookieName = "XSRF-TOKEN";
            TokenHeaderName = "X-XSRF-TOKEN";
            AuthorizationCookieName = ".AspNet.ApplicationCookie";
            AuthorizationCookieApplicationScheme = "Identity.Application";
        }
    }
}