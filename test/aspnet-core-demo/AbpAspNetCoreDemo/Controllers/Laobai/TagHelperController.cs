using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbpAspNetCoreDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AbpAspNetCoreDemo.Controllers.Laobai
{
    public class TagHelperController : DemoControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 这里开始的方法名叫ProcessAsync，但是框架貌似把Async自动去掉了，不能直接访问/taghelper/processasync这个路由地址。
        /// 需要在纯MVC代码里试试，看是abp处理的还是.net core本身处理的
        /// </summary>
        /// <returns></returns>
        public IActionResult ProcessAsyncTag()
        {
            return View();
        }

        public IActionResult Bold()
        {
            return View();
        }

        public IActionResult WebsiteInformation(bool approved = false)
        {
            var context = new WebsiteContext()
            {
                Approved = approved,
                CopyrightYear = 2020,
                TagsToShow = 3,
                Version = new Version(1, 1, 1)

            };
            return View(context);
        }
    }
}
