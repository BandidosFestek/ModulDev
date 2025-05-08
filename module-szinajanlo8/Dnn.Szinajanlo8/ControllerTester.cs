using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using Moq;
using System.Web;
using System.Drawing;
using System.IO;
//using System.Web.Mvc;
using Szinajanlo8.Dnn.Dnn.Szinajanlo8.Controllers;
using Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models;
//using DotNetNuke.Data;
using System.Web.Mvc;
using DotNetNuke.Data;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models;
using System.Reflection;



namespace unittesztmodul
{
    public class ControllerTester
    {
        private static Bitmap CreateBitmapWithColors(Color[,] colors)
        {
            int width = colors.GetLength(0);
            int height = colors.GetLength(1);
            var bmp = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    bmp.SetPixel(x, y, colors[x, y]);
            return bmp;
        }

        private Color InvokeGetAverageColor(object controller, Bitmap bmp, Color wallColor)
        {
            var method = controller.GetType()
                .GetMethod("GetAverageColor", BindingFlags.NonPublic | BindingFlags.Instance);

            return (Color)method.Invoke(controller, new object[] { bmp, wallColor });
        }

        [Test]
        public void GetAverageColor_WithTwoDistantPixels_ReturnsAverage()
        {
            // Arrange
            var controller = new Szinajanlo8.Dnn.Dnn.Szinajanlo8.Controllers.ImageAnalyzerController();

            var colors = new Color[2, 1]
            {
            { Color.Red },  // (255, 0, 0)
            { Color.Green } // (0, 255, 0)
            };

            var bmp = CreateBitmapWithColors(colors);
            var wallColor = Color.Blue; // RGB(0,0,255)

            // Act
            var result = InvokeGetAverageColor(controller, bmp, wallColor);

            // Assert
            var expected = Color.FromArgb(127, 64, 0);
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        public void GetAverageColor_AllPixelsAreWallColor_ReturnsWallColor()
        {
            // Arrange
            var controller = new Szinajanlo8.Dnn.Dnn.Szinajanlo8.Controllers.ImageAnalyzerController();
            var wallColor = Color.FromArgb(100, 100, 100);

            var colors = new Color[2, 2]
            {
            { wallColor, wallColor },
            { wallColor, wallColor }
            };

            var bmp = CreateBitmapWithColors(colors);

            // Act
            var result = InvokeGetAverageColor(controller, bmp, wallColor);

            // Assert
            ClassicAssert.AreEqual(wallColor, result);
        }

        [TestCase(101, 101, 101, 255, 0, 0, 100, 100, 100, 255, 0, 0)]
        public void GetAverageColor_MixedPixels_IgnoresCloseToWallColor(
            int r1, int g1, int b1,
            int r2, int g2, int b2,
            int wr, int wg, int wb,
            int er, int eg, int eb
        )
        {
            // Arrange
            var controller = new Szinajanlo8.Dnn.Dnn.Szinajanlo8.Controllers.ImageAnalyzerController();
            var wallColor = Color.FromArgb(wr, wg, wb);

            var colors = new Color[1, 2]
            {
            {
                Color.FromArgb(r1, g1, b1),
                Color.FromArgb(r2, g2, b2)
            }
            };

            var bmp = CreateBitmapWithColors(colors);

            // Act
            var result = InvokeGetAverageColor(controller, bmp, wallColor);

            // Assert
            var expected = Color.FromArgb(er, eg, eb);
            ClassicAssert.AreEqual(expected, result);
        }

        private ImageAnalyzerController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ImageAnalyzerController();
        }

        [Test]
        public void Index_Post_NullImage_ShowsError()
        {
            // Act
            var result = _controller.Index(null, "#FFFFFF") as ViewResult;

            // Assert
            ClassicAssert.AreEqual("Result", result.ViewName);
            ClassicAssert.IsNotNull(_controller.ViewBag);
            ClassicAssert.IsTrue(_controller.ViewBag.Error.ToString().Contains("Nem töltöttél fel képet"));
        }

        [Test]
        public void Index_Post_ExceptionThrown_ShowsError()
        {
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1);
            mockFile.Setup(f => f.InputStream).Throws(new Exception("Teszt hiba"));

            var result = _controller.Index(mockFile.Object, "#FFFFFF") as ViewResult;

            ClassicAssert.AreEqual("Result", result.ViewName);
            ClassicAssert.IsTrue(_controller.ViewBag.Error.ToString().Contains("Hiba történt"));
        }

        [TestCase("#FF0000")] // Piros
        [TestCase("#00FF00")] // Zöld
        [TestCase("#0000FF")] // Kék
        public void Index_Post_ValidImage_ReturnsSuggestions(string wallColor)
        {
            // Arrange
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.Blue);
            bitmap.SetPixel(1, 0, Color.Green);
            bitmap.SetPixel(0, 1, Color.Red);
            bitmap.SetPixel(1, 1, Color.Black);

            var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;

            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns((int)stream.Length);
            mockFile.Setup(f => f.InputStream).Returns(stream);

            // Bővített színlista – lefedi a legtöbb HSL-változatot
            var colorList = new List<Colors>
    {
        new Colors { R = 255, G = 0, B = 0, SKU = "1" },     // Piros
        new Colors { R = 0, G = 255, B = 0, SKU = "2" },     // Zöld
        new Colors { R = 0, G = 0, B = 255, SKU = "3" },     // Kék
        new Colors { R = 255, G = 255, B = 0, SKU = "4" },   // Sárga
        new Colors { R = 0, G = 255, B = 255, SKU = "5" },   // Cián
        new Colors { R = 255, G = 0, B = 255, SKU = "6" },   // Magenta
        new Colors { R = 128, G = 128, B = 128, SKU = "7" }  // Szürke
    };

            var colorMock = new Mock<IRepository<Colors>>();
            colorMock.Setup(c => c.Get()).Returns(colorList);

            var productMock = new Mock<IRepository<Product>>();
            productMock.Setup(p => p.GetById(It.IsAny<string>()))
                .Returns<string>(sku => new Product { bvin = sku });

            var searchMock = new Mock<IRepository<SearchObjects>>();
            searchMock.Setup(s => s.Get())
                .Returns(colorList.Select(c => new SearchObjects { ObjectId = c.SKU }).AsQueryable());

            var mockContext = new Mock<IDataContext>();
            mockContext.Setup(c => c.GetRepository<Colors>()).Returns(colorMock.Object);
            mockContext.Setup(c => c.GetRepository<Product>()).Returns(productMock.Object);
            mockContext.Setup(c => c.GetRepository<SearchObjects>()).Returns(searchMock.Object);


            // Act
            var result = _controller.Index(mockFile.Object, wallColor) as ViewResult;

            // Assert
            ClassicAssert.AreEqual("Result", result.ViewName);
            ClassicAssert.IsNotNull(_controller.ViewBag.AverageColor);
            ClassicAssert.IsNotNull(_controller.ViewBag.Suggestions);
            ClassicAssert.IsTrue((_controller.ViewBag.Suggestions as List<Tuple<Color, Product, SearchObjects>>).Count > 0);
        }
    }
}
