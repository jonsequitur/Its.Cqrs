using System.Web.Mvc;

namespace Sample.Domain.Api.Controllers
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            return View();
        }
    }
}