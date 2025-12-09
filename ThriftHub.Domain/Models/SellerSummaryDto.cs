using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThriftHub.Domain.Models
{
    public class SellerSummaryDto
    {
        public int SellerID { get; set; }
        public string SellerName { get; set; }
        public string SellerEmail { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalProductsListed { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
