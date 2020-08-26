using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using static System.Int32;

namespace AbpAspNetCoreDemo.Laobai
{
    public class CustomModelBindingModel : IModelBinder
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var nameValueResult = bindingContext.ValueProvider.GetValue(nameof(Name));
            Name = nameValueResult.FirstValue;

            var ageValueResult = bindingContext.ValueProvider.GetValue(nameof(Age)).FirstValue;
            if (!ageValueResult.IsNullOrEmpty())
            {
                if (TryParse(ageValueResult, out var age))
                {
                    Age = age;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(this);//这个是关键，给绑定上下文的结果赋值。
            return Task.CompletedTask;
        }
    }
}
