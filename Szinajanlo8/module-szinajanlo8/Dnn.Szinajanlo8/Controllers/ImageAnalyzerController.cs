using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;

namespace Szinajanlo8.Dnn.Dnn.Szinajanlo8.Controllers
{
    [DnnHandleError]
    public class ImageAnalyzerController : DnnController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)] // csak ha szöveges input lenne, de ártani nem árt
        public ActionResult Index(HttpPostedFileBase imageFile)
        {
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                try
                {
                    using (var bitmap = new Bitmap(imageFile.InputStream))
                    {
                        Color avg = GetAverageColor(bitmap);
                        ViewBag.AverageColor = $"RGB({avg.R}, {avg.G}, {avg.B})";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.AverageColor = "⚠️ Hibás kép vagy feldolgozási hiba.";
                }
            }
            else
            {
                ViewBag.AverageColor = "⚠️ Nem töltöttél fel képet.";
            }

            return View();
        }

        private Color GetAverageColor(Bitmap bmp)
        {
            long r = 0, g = 0, b = 0;
            int total = bmp.Width * bmp.Height;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                }
            }

            return Color.FromArgb((int)(r / total), (int)(g / total), (int)(b / total));
        }
    }
}
