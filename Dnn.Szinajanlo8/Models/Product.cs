using DotNetNuke.ComponentModel.DataAnnotations;
using System;

namespace Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models
{
    [TableName("hcc_Product")]
    [PrimaryKey(nameof(SKU), AutoIncrement = false)]
    public class Product
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string bvin { get; set; }
        public string ImageFileSmall { get; set; }
        public string ImageFileMedium { get; set; }

        public decimal ListPrice { get; set; }
        public decimal SitePrice { get; set; }

        public string TemplateName { get; set; }
        public string RewriteUrl { get; set; }

        public int Status { get; set; }

        public DateTime CreationDate { get; set; }

        public long ProductTypeId { get; set; }
    }
}