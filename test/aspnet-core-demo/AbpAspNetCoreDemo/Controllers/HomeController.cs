using System.Diagnostics;
using Abp;
using Abp.Application.Services;
using Abp.AspNetCore;
using Abp.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AbpAspNetCoreDemo.Controllers
{
    public class HomeController : DemoControllerBase
    {
        //IConfigurationSource、IConfigurationProvider、IConfigurationBuilder、IConfiguration
        //https://blog.csdn.net/wnvalentin/article/details/84956975
        //IConfigurationRoot 继承自 IConfiguration
        private readonly IConfigurationRoot _configuration;
        private readonly IConfiguration _otherConfiguration;
        public HomeController(IConfigurationRoot configuration,
            IConfiguration otherConfiguration)
        {
            _configuration = configuration;
            _otherConfiguration = otherConfiguration;
        }

        [RemoteService(IsEnabled = false)]
        public IActionResult Index([NotNull]string username)
        {
            //第一个参数是source的名字，也就是各个模块注入的时候指定的名称。当该值没找到时，会报错
            //第二个参数是要找的资源名称，也就是资源文件中配置的键，当该值没找到时，会返回[参数名]，这个括号是可以配置的
            //Configuration.Localization.WrapGivenTextIfNotFound = false;如果配置是这样，那么返回的字符就不会有[]
            var teststring = LocalizationManager.GetString(LocalizationSourceName, "AboutDescription");
            var teststring1 = LocalizationManager.GetString(LocalizationSourceName, "MainMenu1");//本处返回的就是[Main Menu1]
            var teststring2 = LocalizationManager.GetString(LocalizationSourceName, "中文变量");//本处返回的就是[中文变量]
            var testString3 = L("zhongwen");
            AbpDebug.WriteLine($"testString={teststring},testString1={teststring1},testString2={teststring2},testString3={testString3}");
            var configurationConnectionString = _configuration.GetConnectionString("Default");
            var defaultConnectionString = _otherConfiguration.GetConnectionString("Default");
            var culture = Request.Query["culture"];
            _configuration.Reload();
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = L("AboutDescription");

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
