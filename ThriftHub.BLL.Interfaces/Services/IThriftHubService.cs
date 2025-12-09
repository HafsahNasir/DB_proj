using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThriftHub.Domain.Models;

namespace ThriftHub.BLL.Interfaces.Services
{
    public interface IThriftHubService
    {
        // Users
        int RegisterUser(UserDto user);
        UserDto GetUserByEmail(string email);

        // Products
        int AddProduct(ProductDto product);
        IEnumerable<ProductDto> SearchProducts(ProductSearchCriteria criteria);
        ProductDto GetProductById(int productId);



        // Orders
        int PlaceSingleProductOrder(int buyerId, int productId, string paymentMethod);
        int PlaceMultiProductOrder(int buyerId, IEnumerable<int> productIds, string paymentMethod);
        IEnumerable<OrderDto> GetBuyerOrders(int buyerId);

        // Reviews
        void AddReview(ReviewDto review);
        IEnumerable<ReviewDto> GetSellerReviews(int sellerId);

        // Reporting / Views
        IEnumerable<SellerSummaryDto> GetSellerSalesSummary();
        IEnumerable<CategorySummaryDto> GetCategorySalesSummary();
        IEnumerable<CategoryDTO> GetAllCategories();
    }
}
