using Abp.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace AbpAspNetCoreDemo.Controllers
{
    public class HomeController : DemoControllerBase
    {
        public IActionResult Index()
        {
            var culture = Request.Query["culture"];
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
