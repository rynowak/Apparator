using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;
using TestApp.Models;

namespace TestApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult TagHelpers([FromServices] RazorEngine engine)
        {
            var feature = engine.Features.OfType<ITagHelperFeature>().First();
            return Content(JsonConvert.SerializeObject(feature.GetDescriptors(), new RazorDiagnosticJsonConverter(), new TagHelperDescriptorJsonConverter()), "application/json");
        }
    }
}
