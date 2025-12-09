using System;

namespace ThriftHub.Domain.Models
{
    public class ProductDto
    {
        public int ProductID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int SellerID { get; set; }
        public string SellerName { get; set; }
        public decimal Price { get; set; }
        public string Condition { get; set; }
        public DateTime UploadDate { get; set; }
        public string Status { get; set; }
        public string ImageURL { get; set; }
    }
}
