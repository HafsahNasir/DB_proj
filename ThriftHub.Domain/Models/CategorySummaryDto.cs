using System;

namespace ThriftHub.Domain.Models
{
    public class CategorySummaryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int TotalProductsListed { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
