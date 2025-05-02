using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DotNetNuke.Data;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models;

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
        [ValidateInput(false)]
        public ActionResult Index(HttpPostedFileBase imageFile, int wallColorR, int wallColorG, int wallColorB)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
            {
                ViewBag.Error = "⚠️ Nem töltöttél fel képet.";
                return View("Result");
            }

            try
            {
                // A felhasználó által megadott fal színének RGB értékekre bontása
                Color wallColorParsed = Color.FromArgb(wallColorR, wallColorG, wallColorB);

                // Kiírás a ViewBag-be a fal színének RGB értékeinek ellenőrzéséhez
                ViewBag.WallColor = $"RGB({wallColorR}, {wallColorG}, {wallColorB})";

                using (var bitmap = new Bitmap(imageFile.InputStream))
                {
                    // Átlag szín kiszámítása figyelmen kívül hagyva a fal színét
                    Color avgColor = GetAverageColor(bitmap, wallColorParsed);
                    ViewBag.AverageColor = $"RGB({avgColor.R}, {avgColor.G}, {avgColor.B})";
                    ViewBag.BaseColorHex = $"#{avgColor.R:X2}{avgColor.G:X2}{avgColor.B:X2}";

                    // RGB -> HSL átalakítás
                    double h, s, l;
                    RgbToHsl(avgColor, out h, out s, out l);

                    // 4 ajánlott szín generálása
                    var suggestedColors = new List<Color>
            {
                HslToRgb((h + 180) % 360, s, l), // Komplementer szín
                HslToRgb((h + 30) % 360, s, l),  // Analóg szín 1
                HslToRgb((h + 330) % 360, s, l), // Analóg szín 2
                HslToRgb(h, Math.Max(0, s - 0.2), Math.Min(1, l + 0.2)) // Monokróm szín
            };

                    // Termékek betöltése és legközelebbi színek keresése
                    var resultList = new List<Tuple<Color, Product, SearchObjects>>();

                    using (var ctx = DataContext.Instance())
                    {
                        var colorRepo = ctx.GetRepository<Colors>();
                        var productRepo = ctx.GetRepository<Product>();
                        var searchRepo = ctx.GetRepository<SearchObjects>();

                        var allColors = colorRepo.Get();

                        foreach (var recColor in suggestedColors)
                        {
                            Colors closest = null;
                            double minDist = double.MaxValue;

                            foreach (var row in allColors)
                            {
                                double d = Distance(row.ToColor(), recColor);
                                if (d < minDist)
                                {
                                    minDist = d;
                                    closest = row;
                                }
                            }

                            Product prod = null;
                            SearchObjects searchObj = null;
                            if (closest != null)
                            {
                                prod = productRepo.GetById(closest.SKU);
                                searchObj = searchRepo.Get().Where(search => search.ObjectId == prod.bvin).FirstOrDefault();
                            }

                            // Kép elérési útvonalának kiszámítása Bvin alapján
                            var productImagePath = $"~/Portals/0/Hotcakes/Data/products/{prod.bvin}/medium/{prod.ImageFileSmall}";

                            // Kép URL dinamikus hozzárendelése ViewBag-en keresztül
                            ViewBag.ProductImageUrl = Url.Content(productImagePath);

                            resultList.Add(Tuple.Create(recColor, prod, searchObj));
                        }

                        ViewBag.Suggestions = resultList;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "⚠️ Hiba történt: " + ex.Message;
            }

            return View("Result");
        }

        private Color GetAverageColor(Bitmap bmp, Color wallColor)
        {
            long r = 0, g = 0, b = 0;
            int total = 0;

            // Kép minden pixelét ellenőrizzük
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);

                    // Színkülönbség számítása a fal színétől
                    double distance = Distance(pixel, wallColor);

                    // Ha a pixel túl közel van a fal színéhez, akkor ne vegyük bele
                    if (distance > 50) // A tolerancia itt állítható
                    {
                        r += pixel.R;
                        g += pixel.G;
                        b += pixel.B;
                        total++;
                    }
                }
            }

            // Ha nem találtunk érvényes pixelt, akkor visszaadjuk a fal színét
            if (total == 0)
            {
                return wallColor; // Ha nincs találat, a fal színe kerül visszaadásra
            }

            return Color.FromArgb((int)(r / total), (int)(g / total), (int)(b / total));
        }

        private double Distance(Color a, Color b)
        {
            return Math.Sqrt(
                (a.R - b.R) * (a.R - b.R) +
                (a.G - b.G) * (a.G - b.G) +
                (a.B - b.B) * (a.B - b.B)
            );
        }

        // RGB -> HSL konverzió
        private void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            h = s = l = (max + min) / 2;

            if (max == min)
            {
                h = s = 0;
            }
            else
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

                if (max == r)
                    h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g)
                    h = (b - r) / d + 2;
                else
                    h = (r - g) / d + 4;

                h *= 60;
            }
        }

        // HSL -> RGB konverzió
        private Color HslToRgb(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 120);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 120);
            }

            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private double HueToRgb(double p, double q, double t)
        {
            while (t < 0) t += 360;
            while (t > 360) t -= 360;

            if (t < 60) return p + (q - p) * t / 60;
            if (t < 180) return q;
            if (t < 240) return p + (q - p) * (240 - t) / 60;
            return p;
        }
    }
}