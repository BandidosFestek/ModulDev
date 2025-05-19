using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        public ActionResult Index(HttpPostedFileBase imageFile, string wallColor)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
            {
                ViewBag.Error = "⚠️ Nem töltöttél fel képet.";
                return View("Result");
            }

            try
            {

                using (var ms = new MemoryStream())
                {
                    imageFile.InputStream.Position = 0; // biztos ami biztos
                    imageFile.InputStream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(bytes);
                    var mimeType = imageFile.ContentType;
                    ViewBag.UploadedImageDataUrl = $"data:{mimeType};base64,{base64}";

                    // Fontos: reset stream pozíció, hogy a Bitmap is használni tudja
                    imageFile.InputStream.Position = 0;
                }
                Color wallColorParsed = ColorTranslator.FromHtml(wallColor);

                using (var bitmap = new Bitmap(imageFile.InputStream))
                {
                    // Átlag szín számítás
                    Color avgColor = GetAverageColor(bitmap, wallColorParsed);
                    ViewBag.BaseColor = avgColor; // konkrét Color típus Razorhoz
                    ViewBag.AverageColor = string.Format("RGB({0}, {1}, {2})", avgColor.R, avgColor.G, avgColor.B);
                    ViewBag.BaseColorHex = string.Format("#{0:X2}{1:X2}{2:X2}", avgColor.R, avgColor.G, avgColor.B);

                    // RGB -> HSL
                    double h, s, l;
                    RgbToHsl(avgColor, out h, out s, out l);

                    var suggestedColors = new List<Color>
                    {
                        HslToRgb((h + 180) % 360, s, l),
                        HslToRgb((h + 30) % 360, s, l),
                        HslToRgb((h + 330) % 360, s, l),
                        HslToRgb(h, Math.Max(0, s - 0.2), Math.Min(1, l + 0.2))
                    };

                    // Egységesített ajánlás
                    var suggestionDict = new Dictionary<string, Tuple<Product, SearchObjects, List<Color>>>();
                    var usedColors = new HashSet<string>(); // RGB színkód string formában

                    using (var ctx = DataContext.Instance())
                    {
                        var colorRepo = ctx.GetRepository<Colors>();
                        var productRepo = ctx.GetRepository<Product>();
                        var searchRepo = ctx.GetRepository<SearchObjects>();

                        var allColors = colorRepo.Get();

                        foreach (var recColor in suggestedColors)
                        {
                            // Színkulcs mint RGB string
                            string rgbKey = $"{recColor.R}-{recColor.G}-{recColor.B}";
                            if (usedColors.Contains(rgbKey))
                                continue; // már használtunk egy SKU-t erre az RGB-re

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

                            if (closest != null)
                            {
                                Product prod = productRepo.GetById(closest.SKU);
                                if (prod == null) continue;

                                SearchObjects searchObj = searchRepo.Get().FirstOrDefault(so => so.ObjectId == prod.bvin);
                                string productKey = prod.bvin;

                                if (!suggestionDict.ContainsKey(productKey))
                                {
                                    suggestionDict[productKey] = Tuple.Create(prod, searchObj, new List<Color> { recColor });
                                }
                                else
                                {
                                    suggestionDict[productKey].Item3.Add(recColor);
                                }

                                usedColors.Add(rgbKey); // ezzel jelezzük, hogy ez az RGB már lefedett
                            }
                        }

                        ViewBag.Suggestions = suggestionDict.Values.ToList();
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

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    double distance = Distance(pixel, wallColor);
                    if (distance > 65)
                    {
                        r += pixel.R;
                        g += pixel.G;
                        b += pixel.B;
                        total++;
                    }
                }
            }

            if (total == 0)
                return wallColor;

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