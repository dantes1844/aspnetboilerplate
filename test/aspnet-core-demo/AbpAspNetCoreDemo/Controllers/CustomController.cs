using Microsoft.AspNetCore.Mvc;

namespace AbpAspNetCoreDemo.Controllers
{
    [Route("api/Custom")]
    [ApiExplorerSettings(IgnoreApi = false)]
    /*
     [Route("api/Custom")]
      [ApiExplorerSettings(IgnoreApi = false)]
     这两个必须同时出现，才能将一个控制器转成api接口。单独将控制器启用ApiExplorerSettings会报错，异常信息：
     InvalidOperationException: The action 'AbpAspNetCoreDemo.Controllers.ProductsController.UglyActionNameForSearch (AbpAspNetCoreDemo)' has ApiExplorer enabled, but is using conventional routing. Only actions which use attribute routing support ApiExplorer.

     */
    public class CustomController : DemoControllerBase
    {
        [Route("action-one")]
        public IActionResult Action1()
        {
            return Content("42");
        }
    }
}