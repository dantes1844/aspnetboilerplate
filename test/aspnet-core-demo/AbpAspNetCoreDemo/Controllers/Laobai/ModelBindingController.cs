using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Extensions;
using AbpAspNetCoreDemo.Laobai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpAspNetCoreDemo.Controllers
{
    public class ModelBindingController : DemoControllerBase
    {
        //[ModelBinder]
        [BindProperty(SupportsGet = true, Name = "name")]//默认不支持Get请求的绑定，需设置属性。另外这个与ModelBinder的差异还要看一下
        public string YujianName { get; set; }

        public string Name { get; set; }

        //[BindRequired] //加上这个之后，就要求必须有值了，否则ModelState.IsValid返回false，并且报异常
        public int Age { get; set; }

        [BindProperty(SupportsGet = true, BinderType = typeof(CustomModelBindingModel))]
        public CustomModelBindingModel CustomModelBindingModel { get; set; }

        public IActionResult Index()
        {
            return Content("Model binding...");
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
            if (CustomModelBindingModel != null)
            {
                AbpDebug.WriteLine($"IModelBinder {nameof(CustomModelBindingModel.Name)}={CustomModelBindingModel.Name},{nameof(CustomModelBindingModel.Age)}={CustomModelBindingModel.Age}");
                return Content(CustomModelBindingModel.ToString());
            }

            return Content("模型自定义类绑定:未能绑定模型");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ModelBindingFromBody([FromBody] Person person)
        {
            return Content(person.ToString());
        }
    }
}
