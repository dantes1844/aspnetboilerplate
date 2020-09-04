using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AbpAspNetCoreDemo.TagHelpers
{
    //[HtmlTargetElement(Attributes = "bold")]//这个是渲染包含bold属性的元素，单独的bold元素是不会被改变
    //[HtmlTargetElement("bold")]//这个与上面刚好相反，只渲染bold标签，包含属性的不会处理<p bold>test</p>//这一行注释掉也是一样的效果。所以默认应该是按照标签进行处理
    //若干个HtmlTargetElement标签之间是or的关系，即只要满足一个就执行转换
    //一个HtmlTargetElement标签中的多个属性则是AND关系。
    [HtmlTargetElement("bold",Attributes = "bold")]//<bold bold>双重bold必须是一个标签里多个条件都列出来</bold>
    //HtmlTargetElement的tag参数可以是任意的标签，不一定是HTML本身的
    public class BoldTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.RemoveAll("bold");

            //这里还有另外一个SetContent的方法，那个是添加文本，不会当成HTML元素进行解析
            output.PreContent.SetHtmlContent("<strong>");
            output.PostContent.SetHtmlContent("</strong>");
        }
    }
}
