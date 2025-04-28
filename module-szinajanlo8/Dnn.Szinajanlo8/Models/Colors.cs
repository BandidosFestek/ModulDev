using DotNetNuke.ComponentModel.DataAnnotations;
using System;
using System.Drawing;

namespace Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models
{
    [TableName("Colors")]
    [PrimaryKey(nameof(SKU), AutoIncrement = false)]
    public class Colors
    {
        public string SKU { get; set; }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public Color ToColor()
        {
            return Color.FromArgb(R, G, B);
        }

        public double DistanceTo(Color other)
        {
            return Math.Sqrt(
                Math.Pow(R - other.R, 2) +
                Math.Pow(G - other.G, 2) +
                Math.Pow(B - other.B, 2)
            );
        }
    }
}