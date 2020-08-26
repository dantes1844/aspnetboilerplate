using System;
using System.Diagnostics;
using Abp;
using Abp.Application.Services;
using Abp.AspNetCore;
using Abp.Localization;
using AbpAspNetCoreDemo.Laobai;
using Castle.Core.Internal;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace AbpAspNetCoreDemo.Controllers
{
    [SurroundClassActionFilter(nameof(HomeController))]
    [BindProperties(SupportsGet = true)]//批量使字段进行绑定
    public class HomeController : DemoControllerBase
    {
        //[ModelBinder]
        [BindProperty(SupportsGet = true, Name = "name")]//默认不支持Get请求的绑定，需设置属性。另外这个与ModelBinder的差异还要看一下
        public string YujianName { get; set; }

        public string Name { get; set; }

        //[BindRequired] //加上这个之后，就要求必须有值了，否则ModelState.IsValid返回false，并且报异常
        public int Age { get; set; }

        [BindProperty(SupportsGet = true, BinderType = typeof(CustomModelBindingModel))]
        public CustomModelBindingModel CustomModelBindingModel { get; set; }

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

        //Startup里已经全局注册了这个，再添加会报错（相同名称的头部已经添加了）
        //这个必须在服务里注册，否则会报错
        //ComponentNotFoundException: No component for supporting the service AbpAspNetCoreDemo.Laobai.GlobalRegisteredActionFilterAttribute was found
        //[ServiceFilter(typeof(GlobalRegisteredActionFilterAttribute))] 
        [TypeFilter(typeof(GlobalRegisteredActionFilterAttribute), Arguments = new object[] { })]//使用TypeFilter时，不注册也不会报上面这个错误。还可以往构造函数里传入参数
        [SurroundClassActionFilter(nameof(Index))]
        [RemoteService(IsEnabled = false)]
        public IActionResult Index([NotNull] string username)
        {
            return View();
        }

        public IActionResult Localization()
        {
            //第一个参数是source的名字，也就是各个模块注入的时候指定的名称。当该值没找到时，会报错
            //第二个参数是要找的资源名称，也就是资源文件中配置的键，当该值没找到时，会返回[参数名]，这个括号是可以配置的
            //Configuration.Localization.WrapGivenTextIfNotFound = false; 如果配置是这样，那么返回的字符就不会有[]
             var teststring = LocalizationManager.GetString(LocalizationSourceName, "AboutDescription");
            var teststring1 = LocalizationManager.GetString(LocalizationSourceName, "MainMenu1");//本处返回的就是[Main Menu1]
            var teststring2 = LocalizationManager.GetString(LocalizationSourceName, "中文变量");//本处返回的就是[中文变量]
            var testString3 = L("zhongwen");
            AbpDebug.WriteLine($"testString={teststring},testString1={teststring1},testString2={teststring2},testString3={testString3}");
            var configurationConnectionString = _configuration.GetConnectionString("Default");
            var defaultConnectionString = _otherConfiguration.GetConnectionString("Default");
            var culture = Request.Query["culture"];
            _configuration.Reload();

            return Content("本地化资源测试");
        }

        public IActionResult ModelBindingValidate()
        {
            AbpDebug.WriteLine($"{Name},{Age}, ModelState.IsValid={ModelState.IsValid}");

            return Content("模型校验绑定");
        }

        public IActionResult ModelBindingProperty()
        {
            if (!YujianName.IsNullOrEmpty())
            {
                AbpDebug.WriteLine($"Property {nameof(YujianName)}={YujianName}");
                AbpDebug.WriteLine($"Properties {nameof(Name)}={Name},{nameof(Age)}={Age}");
            }

            return Content("模型属性绑定");
        }

        public IActionResult ModelBindingCustomClass()
        {
            if (!YujianName.IsNullOrEmpty())
            {
                AbpDebug.WriteLine($"Property {nameof(YujianName)}={YujianName}");
                AbpDebug.WriteLine($"Properties {nameof(Name)}={Name},{nameof(Age)}={Age}");
            }

            return Content("模型自定义类绑定");
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

        #region 过滤器

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            AbpDebug.WriteLine($"{nameof(HomeController)}.{nameof(OnActionExecuting)} Executed");
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            AbpDebug.WriteLine($"{nameof(HomeController)}.{nameof(OnActionExecuted)} Executed");
            base.OnActionExecuted(context);
        }

        #endregion
    }
}
