using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThriftHub.Domain.Models
{
    public class ProductSearchCriteria
    {
        public int? CategoryID { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Condition { get; set; }
        public string Keyword { get; set; }
    }
}
