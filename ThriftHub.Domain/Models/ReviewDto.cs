using System;

namespace ThriftHub.Domain.Models
{
    public class ReviewDto
    {
        public int ReviewID { get; set; }
        public int ProductID { get; set; }
        public int BuyerID { get; set; }
        public string BuyerName { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}
