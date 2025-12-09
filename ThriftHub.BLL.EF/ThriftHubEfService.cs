using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.Domain.Models;

namespace ThriftHub.BLL.EF
{
    public class ThriftHubEfService : IThriftHubService
    {
        
        private readonly ThriftHubDBEntities2 _ctx;

        //HELPERS 
        // Map EF entity -> DTO
        private static UserDto ToUserDto(User entity)
        {
            if (entity == null) return null;

            return new UserDto
            {
                UserID = entity.UserID,
                FullName = entity.FullName,
                Email = entity.Email,
                Password = entity.Password,      // ok for assignment only
                Phone = entity.Phone,
                Address = entity.Address,
                Role = entity.Role,
                JoinDate = entity.JoinDate,
                AverageRating = entity.AverageRating
            };
        }

        // Map DTO -> EF entity (for inserts)
        private static User ToUserEntity(UserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = dto.Password,
                Phone = dto.Phone,
                Address = dto.Address,
                Role = dto.Role,
                // Let DB defaults handle JoinDate / AverageRating if they have defaults,
                // or set them explicitly:
                JoinDate = dto.JoinDate == default ? DateTime.Now : dto.JoinDate,
                AverageRating = dto.AverageRating
            };
        }

        // Map EF Product entity -> ProductDto
        private static ProductDto ToProductDto(Product entity)
        {
            if (entity == null) return null;

            return new ProductDto
            {
                ProductID = entity.ProductID,
                Title = entity.Title,
                Description = entity.Description,
                CategoryID = entity.CategoryID,
                // Use navigation properties if available:
                CategoryName = entity.Category != null ? entity.Category.CategoryName : null,
                SellerID = entity.SellerID,
                SellerName = entity.User != null ? entity.User.FullName : null, // adjust if nav prop name differs
                Price = entity.Price,
                Condition = entity.Condition,
                UploadDate = entity.UploadDate,
                Status = entity.Status,
                ImageURL = entity.ImageURL
            };
        }

        // Map ProductDto -> EF entity (for inserts)
        private static Product ToProductEntity(ProductDto dto)
        {
            if (dto == null) return null;

            return new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                CategoryID = dto.CategoryID,
                SellerID = dto.SellerID,
                Price = dto.Price,
                Condition = dto.Condition,
                // If UploadDate not set, use now
                UploadDate = dto.UploadDate == default ? DateTime.Now : dto.UploadDate,
                // default to "Available" if not provided
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Available" : dto.Status,
                ImageURL = dto.ImageURL
            };
        }

        // Map EF Review -> DTO
        private static ReviewDto ToReviewDto(Review r, string buyerName = null)
        {
            if (r == null) return null;

            return new ReviewDto
            {
                ReviewID = r.ReviewID,
                ProductID = r.ProductID,
                BuyerID = r.BuyerID,
                BuyerName = buyerName,          // we fill this when we join with Users
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                ReviewDate = r.ReviewDate
            };
        }

        // Map vw_OrderDetailsFull group -> OrderDto
        private static OrderDto ToOrderDtoFromGroup(IGrouping<int, vw_OrderDetailsFull> group)
        {
            var first = group.First();

            var order = new OrderDto
            {
                OrderID = first.OrderID,
                BuyerID = first.BuyerID,
                BuyerName = first.BuyerName,
                OrderDate = first.OrderDate,
                TotalAmount = first.TotalAmount,
                OrderStatus = first.OrderStatus,
                OrderDetails = new List<OrderDetailDto>()
            };

            foreach (var row in group)
            {
                order.OrderDetails.Add(new OrderDetailDto
                {
                    OrderDetailID = row.OrderDetailID,
                    OrderID = row.OrderID,
                    ProductID = row.ProductID,
                    ProductTitle = row.ProductTitle,
                    Amount = row.Amount,
                    PaymentMethod = row.PaymentMethod,
                    PaymentStatus = row.PaymentStatus,
                    PaymentDate = row.PaymentDate,
                    SellerID = row.SellerID,
                    SellerName = row.SellerName
                });
            }

            return order;
        }

        // FUNCTIONS 
        public ThriftHubEfService()
        {
            _ctx = new ThriftHubDBEntities2();
        }

        // Users
        public int RegisterUser(UserDto user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Basic validation – you can extend this later
            if (string.IsNullOrWhiteSpace(user.FullName))
                throw new ArgumentException("Full name is required.", nameof(user));

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Email is required.", nameof(user));

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new ArgumentException("Password is required.", nameof(user));

            // Check if email already exists
            var existing = _ctx.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existing != null)
                throw new InvalidOperationException("A user with this email already exists.");

            // Map DTO -> EF entity
            var entity = ToUserEntity(user);

            // If your DB has default JoinDate/AverageRating you can set them here:
            if (entity.JoinDate == default)
                entity.JoinDate = DateTime.Now;

            // Add and save
            _ctx.Users.Add(entity);
            _ctx.SaveChanges();

            // EF fills the identity key after SaveChanges()
            return entity.UserID;
        }

        public UserDto GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var entity = _ctx.Users.FirstOrDefault(u => u.Email == email);

            return ToUserDto(entity);
        }

        // Products
        public int AddProduct(ProductDto product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.Title))
                throw new ArgumentException("Title is required.", nameof(product));

            if (product.CategoryID <= 0)
                throw new ArgumentException("Valid CategoryID is required.", nameof(product));

            if (product.SellerID <= 0)
                throw new ArgumentException("Valid SellerID (seller user) is required.", nameof(product));

            if (product.Price <= 0)
                throw new ArgumentException("Price must be greater than zero.", nameof(product));

            // Map DTO -> EF entity
            var entity = ToProductEntity(product);

            _ctx.Products.Add(entity);
            _ctx.SaveChanges();

            // DB fills identity key after SaveChanges
            return entity.ProductID;
        }

        public IEnumerable<ProductDto> SearchProducts(ProductSearchCriteria criteria)
        {
            if (criteria == null)
                criteria = new ProductSearchCriteria();

            // Start with all available products
            var query = _ctx.Products
                            .Include("Category")
                            .Include("User")
                            .AsQueryable();

            // We usually only show available items in search:
            query = query.Where(p => p.Status == "Available");

            // Filter by category
            if (criteria.CategoryID.HasValue && criteria.CategoryID.Value > 0)
            {
                int catId = criteria.CategoryID.Value;
                query = query.Where(p => p.CategoryID == catId);
            }

            // Filter by min price
            if (criteria.MinPrice.HasValue)
            {
                decimal min = criteria.MinPrice.Value;
                query = query.Where(p => p.Price >= min);
            }

            // Filter by max price
            if (criteria.MaxPrice.HasValue)
            {
                decimal max = criteria.MaxPrice.Value;
                query = query.Where(p => p.Price <= max);
            }

            // Filter by condition (e.g. "New", "Used")
            if (!string.IsNullOrWhiteSpace(criteria.Condition))
            {
                string cond = criteria.Condition;
                query = query.Where(p => p.Condition == cond);
            }

            // Keyword search in title or description
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                string kw = criteria.Keyword;
                query = query.Where(p =>
                    p.Title.Contains(kw) ||
                    p.Description.Contains(kw));
            }

            // Execute query and map to DTOs
            var results = query
                .OrderByDescending(p => p.UploadDate)
                .ToList()
                .Select(p => ToProductDto(p))
                .ToList();

            return results;
        }

        public ProductDto GetProductById(int productId)
        {
            if (productId <= 0)
                return null;

            // Load product + related Category & User (seller)
            var entity = _ctx.Products
                             .Include("Category")
                             .Include("User")
                             .FirstOrDefault(p => p.ProductID == productId);

            return ToProductDto(entity);
        }

        // Orders
        public int PlaceSingleProductOrder(int buyerId, int productId, string paymentMethod)
        {
            if (buyerId <= 0) throw new ArgumentException("Invalid buyer id.", nameof(buyerId));
            if (productId <= 0) throw new ArgumentException("Invalid product id.", nameof(productId));
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required.", nameof(paymentMethod));

            var buyer = _ctx.Users.AsNoTracking().FirstOrDefault(u => u.UserID == buyerId);
            if (buyer == null)
                throw new InvalidOperationException("Buyer not found.");

            var product = _ctx.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == productId && p.Status == "Available");
            if (product == null)
                throw new InvalidOperationException("Product not found or not available.");

            using (var tx = _ctx.Database.BeginTransaction())
            {
                // Use raw SQL to insert Order and capture the exact OrderID and OrderDate
                var sql = @"
                    INSERT INTO Orders (BuyerID, OrderDate, TotalAmount, Status)
                    VALUES (@p0, GETDATE(), @p1, @p2);
                    SELECT CAST(SCOPE_IDENTITY() AS INT) AS OrderID, 
                           (SELECT OrderDate FROM Orders WHERE OrderID = SCOPE_IDENTITY()) AS OrderDate;";

                var result = _ctx.Database.SqlQuery<OrderIdDateResult>(
                    sql,
                    buyerId,
                    product.Price,
                    "Paid"
                ).FirstOrDefault();

                if (result == null)
                    throw new InvalidOperationException("Failed to create order.");

                // Insert OrderDetail using the exact OrderDate from the database
                var detailSql = @"
                    INSERT INTO OrderDetails (OrderID, OrderDate, ProductID, PaymentDate, Amount, Method, Status)
                    VALUES (@p0, @p1, @p2, GETDATE(), @p3, @p4, @p5);";

                _ctx.Database.ExecuteSqlCommand(
                    detailSql,
                    result.OrderID,
                    result.OrderDate,
                    product.ProductID,
                    product.Price,
                    paymentMethod,
                    "Paid"
                );

                // Mark product as SOLD
                _ctx.Database.ExecuteSqlCommand(
                    "UPDATE Products SET [Status] = 'Sold' WHERE ProductID = @p0",
                    product.ProductID);

                tx.Commit();
                return result.OrderID;
            }
        }

        public int PlaceMultiProductOrder(int buyerId, IEnumerable<int> productIds, string paymentMethod)
        {
            if (buyerId <= 0) throw new ArgumentException("Invalid buyer id.", nameof(buyerId));
            if (productIds == null) throw new ArgumentNullException(nameof(productIds));

            var ids = productIds.Distinct().ToList();
            if (!ids.Any()) throw new ArgumentException("No product ids provided.", nameof(productIds));
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required.", nameof(paymentMethod));

            var buyer = _ctx.Users.AsNoTracking().FirstOrDefault(u => u.UserID == buyerId);
            if (buyer == null)
                throw new InvalidOperationException("Buyer not found.");

            var products = _ctx.Products
                               .AsNoTracking()
                               .Where(p => ids.Contains(p.ProductID) && p.Status == "Available")
                               .ToList();

            if (products.Count != ids.Count)
                throw new InvalidOperationException("One or more products are missing or not available.");

            using (var tx = _ctx.Database.BeginTransaction())
            {
                decimal total = products.Sum(p => p.Price);

                // Use raw SQL to insert Order and capture exact OrderID and OrderDate
                var sql = @"
                    INSERT INTO Orders (BuyerID, OrderDate, TotalAmount, Status)
                    VALUES (@p0, GETDATE(), @p1, @p2);
                    SELECT CAST(SCOPE_IDENTITY() AS INT) AS OrderID, 
                           (SELECT OrderDate FROM Orders WHERE OrderID = SCOPE_IDENTITY()) AS OrderDate;";

                var result = _ctx.Database.SqlQuery<OrderIdDateResult>(
                    sql,
                    buyerId,
                    total,
                    "Paid"
                ).FirstOrDefault();

                if (result == null)
                    throw new InvalidOperationException("Failed to create order.");

                // Insert all OrderDetails using exact OrderDate
                foreach (var product in products)
                {
                    var detailSql = @"
                        INSERT INTO OrderDetails (OrderID, OrderDate, ProductID, PaymentDate, Amount, Method, Status)
                        VALUES (@p0, @p1, @p2, GETDATE(), @p3, @p4, @p5);";

                    _ctx.Database.ExecuteSqlCommand(
                        detailSql,
                        result.OrderID,
                        result.OrderDate,
                        product.ProductID,
                        product.Price,
                        paymentMethod,
                        "Paid"
                    );
                }

                // Mark all products as SOLD
                foreach (var product in products)
                {
                    _ctx.Database.ExecuteSqlCommand(
                        "UPDATE Products SET [Status] = 'Sold' WHERE ProductID = @p0",
                        product.ProductID);
                }

                tx.Commit();
                return result.OrderID;
            }
        }

        // Helper class for capturing Order ID and Date
        private class OrderIdDateResult
        {
            public int OrderID { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public IEnumerable<OrderDto> GetBuyerOrders(int buyerId)
        {
            if (buyerId <= 0)
                return new List<OrderDto>();

            var rows = _ctx.vw_OrderDetailsFull
                           .Where(r => r.BuyerID == buyerId)
                           .OrderByDescending(r => r.OrderDate)
                           .ToList();

            if (!rows.Any())
                return new List<OrderDto>();

            var grouped = rows.GroupBy(r => r.OrderID);

            var orders = grouped
                .Select(g => ToOrderDtoFromGroup(g))
                .ToList();

            return orders;
        }

        // Reviews
        public void AddReview(ReviewDto review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (review.ProductID <= 0)
                throw new ArgumentException("Valid ProductID is required.", nameof(review.ProductID));

            if (review.BuyerID <= 0)
                throw new ArgumentException("Valid BuyerID is required.", nameof(review.BuyerID));

            if (review.Rating < 1 || review.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.", nameof(review.Rating));

            var product = _ctx.Products.FirstOrDefault(p => p.ProductID == review.ProductID);
            if (product == null)
                throw new InvalidOperationException("Product not found.");

            var buyer = _ctx.Users.FirstOrDefault(u => u.UserID == review.BuyerID);
            if (buyer == null)
                throw new InvalidOperationException("Buyer not found.");

            var entity = new Review
            {
                ProductID = review.ProductID,
                BuyerID = review.BuyerID,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                ReviewDate = review.ReviewDate == default ? DateTime.Now : review.ReviewDate
            };

            _ctx.Reviews.Add(entity);
            _ctx.SaveChanges();
        }

        public IEnumerable<ReviewDto> GetSellerReviews(int sellerId)
        {
            if (sellerId <= 0)
                return new List<ReviewDto>();

            var query =
                from r in _ctx.Reviews
                join p in _ctx.Products on r.ProductID equals p.ProductID
                join b in _ctx.Users on r.BuyerID equals b.UserID
                where p.SellerID == sellerId
                orderby r.ReviewDate descending
                select new ReviewDto
                {
                    ReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    BuyerID = r.BuyerID,
                    BuyerName = b.FullName,
                    Rating = r.Rating,
                    ReviewText = r.ReviewText,
                    ReviewDate = r.ReviewDate
                };

            return query.ToList();
        }

        // Reporting / Views
        public IEnumerable<SellerSummaryDto> GetSellerSalesSummary()
        {
            var rows = _ctx.vw_SellerSalesSummary
                .OrderByDescending(s => s.TotalRevenue)
                .ToList();

            var result = rows.Select(s => new SellerSummaryDto
            {
                SellerID = s.SellerID,
                SellerName = s.SellerName,
                SellerEmail = s.SellerEmail,
                AverageRating = s.SellerAverageRating,
                TotalProductsListed = s.TotalProductsListed.GetValueOrDefault(),
                TotalItemsSold = s.TotalItemsSold.GetValueOrDefault(),
                TotalRevenue = s.TotalRevenue
            }).ToList();

            return result;
        }

        public IEnumerable<CategorySummaryDto> GetCategorySalesSummary()
        {
            var rows = _ctx.vw_CategorySalesSummary
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            var result = rows.Select(c => new CategorySummaryDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                TotalProductsListed = c.TotalProductsListed.GetValueOrDefault(),
                TotalItemsSold = c.TotalItemsSold.GetValueOrDefault(),
                TotalRevenue = c.TotalRevenue
            }).ToList();

            return result;
        }
        //         GET ALL CATEGORIES
        public IEnumerable<CategoryDTO> GetAllCategories()
        {
            return _ctx.Categories
                       .OrderBy(c => c.CategoryName)
                       .Select(c => new CategoryDTO
                       {
                           CategoryID = c.CategoryID,
                           CategoryName = c.CategoryName
                       })
                       .ToList();
        }
    }
}