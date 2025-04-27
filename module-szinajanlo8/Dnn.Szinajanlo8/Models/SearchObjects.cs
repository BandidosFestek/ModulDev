using DotNetNuke.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Szinajanlo8.Dnn.Dnn.Szinajanlo8.Models
{
    [TableName("hcc_SearchObjects")]
    [PrimaryKey(nameof(Id), AutoIncrement = false)]
    public class SearchObjects
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}