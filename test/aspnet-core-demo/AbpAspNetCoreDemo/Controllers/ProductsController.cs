using Abp.Application.Services;
using AbpAspNetCoreDemo.Core.Domain;
using Abp.AspNetCore.OData.Controllers;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AbpAspNetCoreDemo.Controllers
{
    [RemoteService] 
    public class ProductController : AbpODataEntityController<Product>, ITransientDependency
    {
        public ProductController(IRepository<Product> repository) : base(repository)
        {
            GetPermissionName = "GetProductPermission";
            GetAllPermissionName = "GetAllProductsPermission";
            CreatePermissionName = "CreateProductPermission";
            UpdatePermissionName = "UpdateProductPermission";
            DeletePermissionName = "DeleteProductPermission";
        }

        /// <summary>
        /// 服务类有个ProductsAppService，默认会使用它及内部的方法生成api，这里的方法会被忽略。【todo:待验证】
        /// </summary>
        /// <returns></returns>
        [RemoteService]//必须跟控制器同时打上标签才行
        public IActionResult UglyActionNameForSearch()
        {
            return Content("Products Index");
        }

        #region 这个会跟服务端的生成一样地址的api，要避免这种情况出现
        
        //[ApiExplorerSettings(IgnoreApi = false)]
        //[Route("api/services/app/product/GetAll")]
        //[HttpGet]
        //public IActionResult GetProducts()
        //{
        //    return Content(nameof(GetProducts));
        //} 

        #endregion
    }
}
