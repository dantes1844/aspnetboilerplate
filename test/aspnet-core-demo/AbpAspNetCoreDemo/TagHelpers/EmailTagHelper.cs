using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AbpAspNetCoreDemo.TagHelpers
{
    //加上下面这行，会生成一个没有结束标签的tag，造成<a>跟其他的</a>组合成一对，形成错误的超链接
    //[HtmlTargetElement("email", TagStructure = TagStructure.WithoutEndTag)]
    public class EmailTagHelper : TagHelper
    {
        private const string EmailDomain = "contoso.com";

        public string MailTo { get; set; }

        //public override void Process(TagHelperContext context, TagHelperOutput output)
        //{
        //    output.TagName = "a";

        //    var address = $"{MailTo}@{EmailDomain}";
        //    output.Attributes.SetAttribute("href", $"mailto:{address}");
        //    output.Content.SetContent(address);
        //}

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "a";
            var contentTask = await output.GetChildContentAsync();
            var target = contentTask.GetContent() + "@" + EmailDomain;
            output.Attributes.SetAttribute("href",$"mailto:{target}");
            output.Content.SetContent(target);
        }
    }
}
