using GadgetsOnline.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace GadgetsOnline.Controllers
{
    public class HomeController : Controller
    {
        Inventory inventory;
        public ActionResult Index()
        {
            inventory = new Inventory();
            var products = inventory.GetBestSellers(6);
            ViewBag.Hostname = "; Node: " + Environment.MachineName;
            ViewBag.AvailabilityZone = "; AZ: " + GetMetadataValue("placement/availability-zone");
            ViewBag.InstanceType = "; Instance-Type: " + GetMetadataValue("instance-type");
            return View(products);
        }

        private static readonly Dictionary<string, string> metadataCache = new Dictionary<string, string>();

        private static string GetMetadataValue(string path)
        {
            if (!metadataCache.ContainsKey(path))
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync($"http://169.254.169.254/latest/meta-data/{path}").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var metadataValue = response.Content.ReadAsStringAsync().Result;
                        metadataCache[path] = metadataValue;
                    }
                    else
                    {
                        metadataCache[path] = "Unknown";
                    }
                }
            }

            return metadataCache[path];
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}
