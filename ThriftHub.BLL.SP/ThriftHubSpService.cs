using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.Domain.Models;

namespace ThriftHub.BLL.SP
{
    public class ThriftHubSpService : IThriftHubService
    {
        private readonly string _connString;

        public ThriftHubSpService()
        {
            // Uses a normal ADO.NET connection string named "ThriftHubDB"
            // (defined in App.config of the startup project)
            var cs = ConfigurationManager.ConnectionStrings["ThriftHubDB"];
            if (cs == null)
                throw new InvalidOperationException(
                    "Connection string 'ThriftHubDB' not found in config.");

            _connString = cs.ConnectionString;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_connString);
        }

        // =========================================================
        //  USERS
        // =========================================================

        public int RegisterUser(UserDto user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_RegisterUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);
                cmd.Parameters.AddWithValue("@Phone", (object)user.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)user.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Role", user.Role);

                conn.Open();
                var result = cmd.ExecuteScalar();

                // sp_RegisterUser does: SELECT SCOPE_IDENTITY() AS NewUserID;
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);

                // If for some reason no scalar is returned:
                return 0;
            }
        }

        public UserDto GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            const string sql = @"
SELECT UserID, FullName, Email, [Password], Phone, [Address], [Role]
FROM   Users
WHERE  Email = @Email;";

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Email", email);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new UserDto
                    {
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Password = reader.GetString(reader.GetOrdinal("Password")),
                        Phone = reader["Phone"] as string,
                        Address = reader["Address"] as string,
                        Role = reader.GetString(reader.GetOrdinal("Role"))
                    };
                }
            }
        }

        // =========================================================
        //  PRODUCTS
        // =========================================================

        public int AddProduct(ProductDto product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_AddProduct", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SellerID", product.SellerID);
                cmd.Parameters.AddWithValue("@Title", product.Title);
                cmd.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@Condition", product.Condition ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ImageURL", (object)product.ImageURL ?? DBNull.Value);

                conn.Open();
                var result = cmd.ExecuteScalar();

                // sp_AddProduct: SELECT @NewProductID AS ProductID;
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);

                return 0;
            }
        }

        public IEnumerable<ProductDto> SearchProducts(ProductSearchCriteria criteria)
        {
            criteria = criteria ?? new ProductSearchCriteria();

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_SearchProducts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                object cat = criteria.CategoryID.HasValue ? (object)criteria.CategoryID.Value : DBNull.Value;
                object minP = criteria.MinPrice.HasValue ? (object)criteria.MinPrice.Value : DBNull.Value;
                object maxP = criteria.MaxPrice.HasValue ? (object)criteria.MaxPrice.Value : DBNull.Value;
                object cond = string.IsNullOrWhiteSpace(criteria.Condition)
                    ? (object)DBNull.Value
                    : criteria.Condition;
                object keyword = string.IsNullOrWhiteSpace(criteria.Keyword)
                    ? (object)DBNull.Value
                    : criteria.Keyword;

                cmd.Parameters.AddWithValue("@CategoryID", cat);
                cmd.Parameters.AddWithValue("@MinPrice", minP);
                cmd.Parameters.AddWithValue("@MaxPrice", maxP);
                cmd.Parameters.AddWithValue("@Condition", cond);
                cmd.Parameters.AddWithValue("@Keyword", keyword);

                conn.Open();
                var list = new List<ProductDto>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new ProductDto
                        {
                            ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            Description = reader["Description"] as string,
                            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                            Condition = reader["Condition"] as string,
                            ImageURL = reader["ImageURL"] as string,

                            // Your SP only returns "Available" products,
                            // so we can safely set this constant to keep UI behaviour same as EF.
                            Status = "Available",

                            // Extra info from the JOINs:
                            CategoryName = reader["CategoryName"] as string,
                            SellerName = reader["SellerName"] as string
                        };

                        list.Add(dto);
                    }
                }

                return list;
            }
        }
        public ProductDto GetProductById(int productId)
        {
            const string sql = @"
SELECT 
    p.ProductID,
    p.Title,
    p.[Description],
    p.Price,
    p.[Condition],
    p.[Status],
    p.ImageURL,
    p.CategoryID,
    c.CategoryName,
    p.SellerID,
    u.FullName AS SellerName
FROM   Products   p
JOIN   Categories c ON p.CategoryID = c.CategoryID
JOIN   Users      u ON p.SellerID   = u.UserID
WHERE  p.ProductID = @ProductID;";

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductID", productId);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new ProductDto
                    {
                        ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Description = reader["Description"] as string,
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        Condition = reader["Condition"] as string,
                        Status = reader["Status"] as string,
                        ImageURL = reader["ImageURL"] as string,
                        CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                        SellerID = reader.GetInt32(reader.GetOrdinal("SellerID")),
                        SellerName = reader["SellerName"] as string
                        // If ProductDto has CategoryName, you can also map:
                        // CategoryName = reader["CategoryName"] as string
                    };
                }
            }
        }

        // =========================================================
        //  ORDERS
        // =========================================================

        public int PlaceSingleProductOrder(int buyerId, int productId, string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required.", nameof(paymentMethod));

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_PlaceSingleProductOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BuyerID", buyerId);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);

                conn.Open();
                // If the proc SELECTs @OrderID at the end, this will capture it.
                var result = cmd.ExecuteScalar();
                int orderId = (result != null && result != DBNull.Value)
                    ? Convert.ToInt32(result)
                    : 0;

                return orderId;
            }
        }

        private static DataTable BuildProductIdTable(IEnumerable<int> productIds)
        {
            var dt = new DataTable();
            dt.Columns.Add("ProductID", typeof(int));

            if (productIds != null)
            {
                var seen = new HashSet<int>();
                foreach (var id in productIds)
                {
                    if (seen.Add(id))
                        dt.Rows.Add(id);
                }
            }

            return dt;
        }

        public int PlaceMultiProductOrder(int buyerId, IEnumerable<int> productIds, string paymentMethod)
        {
            if (productIds == null)
                throw new ArgumentNullException(nameof(productIds));

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required.", nameof(paymentMethod));

            var dtProducts = BuildProductIdTable(productIds);

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_PlaceMultiProductOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BuyerID", buyerId);

                var tvp = cmd.Parameters.AddWithValue("@Products", dtProducts);
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "ProductIdList";

                cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);

                conn.Open();
                var result = cmd.ExecuteScalar();
                int orderId = (result != null && result != DBNull.Value)
                    ? Convert.ToInt32(result)
                    : 0;

                return orderId;
            }
        }

        public IEnumerable<OrderDto> GetBuyerOrders(int buyerId)
        {
            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_GetBuyerOrders", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BuyerID", buyerId);

                conn.Open();
                var orders = new Dictionary<int, OrderDto>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int orderId = reader.GetInt32(reader.GetOrdinal("OrderID"));

                        if (!orders.TryGetValue(orderId, out var order))
                        {
                            order = new OrderDto
                            {
                                OrderID = orderId,
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                OrderStatus = reader["OrderStatus"] as string,
                                OrderDetails = new List<OrderDetailDto>()
                            };
                            orders.Add(orderId, order);
                        }

                        var detail = new OrderDetailDto
                        {
                            OrderDetailID = reader.GetInt32(reader.GetOrdinal("OrderDetailID")),
                            ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                            ProductTitle = reader["ProductTitle"] as string,
                            Amount = reader.GetDecimal(reader.GetOrdinal("ProductPrice")),
                            PaymentMethod = reader["PaymentMethod"] as string,
                            PaymentStatus = reader["PaymentStatus"] as string,
                            SellerID = reader.GetInt32(reader.GetOrdinal("SellerID")),
                            SellerName = reader["SellerName"] as string
                        };

                        order.OrderDetails.Add(detail);
                    }
                }

                return orders.Values;
            }
        }

        // =========================================================
        //  REVIEWS
        // =========================================================

        public void AddReview(ReviewDto review)
        {
            if (review == null) throw new ArgumentNullException(nameof(review));

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_AddReview", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProductID", review.ProductID);
                cmd.Parameters.AddWithValue("@BuyerID", review.BuyerID);
                cmd.Parameters.AddWithValue("@Rating", review.Rating);
                cmd.Parameters.AddWithValue("@ReviewText",
                    (object)review.ReviewText ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<ReviewDto> GetSellerReviews(int sellerId)
        {
            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand("sp_GetSellerReviews", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SellerID", sellerId);

                conn.Open();
                var list = new List<ReviewDto>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new ReviewDto
                        {
                            ReviewID = reader.GetInt32(reader.GetOrdinal("ReviewID")),
                            Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                            ReviewText = reader["ReviewText"] as string,
                            ReviewDate = reader.GetDateTime(reader.GetOrdinal("ReviewDate")),
                            ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                            BuyerName = reader["BuyerName"] as string
                        };

                        list.Add(dto);
                    }
                }

                return list;
            }
        }

        // =========================================================
        //  REPORTING / VIEWS
        // =========================================================

        public IEnumerable<SellerSummaryDto> GetSellerSalesSummary()
        {
            const string sql = @"
SELECT 
    SellerID,
    SellerName,
    TotalItemsSold   = TotalItemsSold,
    TotalRevenue     = TotalRevenue
FROM vw_SellerSalesSummary
ORDER BY TotalRevenue DESC;";

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                conn.Open();

                var list = new List<SellerSummaryDto>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new SellerSummaryDto
                        {
                            SellerID = reader.GetInt32(reader.GetOrdinal("SellerID")),
                            SellerName = reader["SellerName"] as string,
                            TotalItemsSold = reader.GetInt32(reader.GetOrdinal("TotalItemsSold")),
                            TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))
                        };

                        list.Add(dto);
                    }
                }

                return list;
            }
        }

        public IEnumerable<CategorySummaryDto> GetCategorySalesSummary()
        {
            const string sql = @"
SELECT 
    CategoryID,
    CategoryName,
    TotalItemsSold   = TotalItemsSold,
    TotalRevenue     = TotalRevenue
FROM vw_CategorySalesSummary
ORDER BY CategoryName;";

            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                conn.Open();

                var list = new List<CategorySummaryDto>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new CategorySummaryDto
                        {
                            CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                            CategoryName = reader["CategoryName"] as string,
                            TotalItemsSold = reader.GetInt32(reader.GetOrdinal("TotalItemsSold")),
                            TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))
                        };

                        list.Add(dto);
                    }
                }

                return list;
            }
        }

        public IEnumerable<CategoryDTO> GetAllCategories()
        {
            var list = new List<CategoryDTO>();

            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CategoryDTO
                        {
                            CategoryID = reader.GetInt32(0),
                            CategoryName = reader.GetString(1)
                        });
                    }
                }
            }

            return list;
        }
    }
}
