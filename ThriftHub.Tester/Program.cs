using System;
using System.Collections.Generic;
using ThriftHub.BLL.EF;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.BLL.SP;       // <-- ADD THIS
using ThriftHub.Domain.Models;

namespace ThriftHub.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // SWITCH HERE:
            // var service = new ThriftHubSpService();
            var service = new ThriftHubSpService();   // <-- TEST SP SERVICE NOW

            Console.WriteLine("=== Testing ThriftHubSpService ===");

            try
            {
                // 1. Users
                Console.WriteLine("\n--- 1) Users: Ensure buyer & seller exist ---");
                int buyerId = EnsureUserExists(service,
                    email: "buyer.test@example.com",
                    fullName: "Test Buyer",
                    role: "BuyerSeller");

                int sellerId = EnsureUserExists(service,
                    email: "seller.test@example.com",
                    fullName: "Test Seller",
                    role: "BuyerSeller");

                Console.WriteLine($"BuyerID: {buyerId}, SellerID: {sellerId}");

                // 2. Products
                Console.WriteLine("\n--- 2) Products: Add & fetch ---");

                const int testCategoryId = 1;

                int prod1Id = service.AddProduct(new ProductDto
                {
                    Title = "Test Product 1",
                    Description = "First test product",
                    CategoryID = testCategoryId,
                    SellerID = sellerId,
                    Price = 1500m,
                    Condition = "New",
                    Status = "Available"
                });

                int prod2Id = service.AddProduct(new ProductDto
                {
                    Title = "Test Product 2",
                    Description = "Second test product",
                    CategoryID = testCategoryId,
                    SellerID = sellerId,
                    Price = 2500m,
                    Condition = "New",
                    Status = "Available"
                });

                Console.WriteLine($"Added products: {prod1Id}, {prod2Id}");

                var prod1 = service.GetProductById(prod1Id);
                Console.WriteLine($"GetProductById({prod1Id}) => {prod1.Title}, Rs {prod1.Price}, Status {prod1.Status}");

                // 3. Search
                Console.WriteLine("\n--- 3) SearchProducts ---");
                var searchResults = service.SearchProducts(new ProductSearchCriteria
                {
                    Keyword = "Test Product",
                    CategoryID = testCategoryId
                });

                foreach (var p in searchResults)
                {
                    Console.WriteLine($"Found: {p.ProductID} - {p.Title} - Rs {p.Price} - Seller {p.SellerName}");
                }

                // 4. Orders
                Console.WriteLine("\n--- 4) Orders: Place single & multi-product orders ---");

                int order1Id = service.PlaceSingleProductOrder(buyerId, prod1Id, "CashOnDelivery");
                Console.WriteLine($"PlaceSingleProductOrder => OrderID {order1Id}");

                int prod3Id = service.AddProduct(new ProductDto
                {
                    Title = "Test Product 3",
                    Description = "Third test product",
                    CategoryID = testCategoryId,
                    SellerID = sellerId,
                    Price = 3500m,
                    Condition = "New",
                    Status = "Available"
                });

                int order2Id = service.PlaceMultiProductOrder(
                    buyerId,
                    new List<int> { prod2Id, prod3Id },
                    "CashOnDelivery");

                Console.WriteLine($"PlaceMultiProductOrder => OrderID {order2Id}");

                // 5. Orders list
                Console.WriteLine("\n--- 5) GetBuyerOrders ---");
                var buyerOrders = service.GetBuyerOrders(buyerId);

                foreach (var o in buyerOrders)
                {
                    Console.WriteLine($"\nOrder {o.OrderID} on {o.OrderDate}, Total = {o.TotalAmount}, Status = {o.OrderStatus}");
                    if (o.OrderDetails != null)
                    {
                        foreach (var d in o.OrderDetails)
                        {
                            Console.WriteLine($"   - Product {d.ProductID}, Amount {d.Amount}, Seller {d.SellerName}");
                        }
                    }
                }

                // 6. Reviews
                Console.WriteLine("\n--- 6) Reviews: Add & fetch ---");

                var review = new ReviewDto
                {
                    ProductID = prod2Id,
                    BuyerID = buyerId,
                    Rating = 5,
                    ReviewText = "Great product!",
                    ReviewDate = DateTime.Now
                };

                service.AddReview(review);
                Console.WriteLine($"Added review for Product {prod2Id} by Buyer {buyerId}");

                var sellerReviews = service.GetSellerReviews(sellerId);

                foreach (var r in sellerReviews)
                {
                    Console.WriteLine($"Review {r.ReviewID} - Rating {r.Rating} - '{r.ReviewText}'");
                }

                // 7. Reporting
                Console.WriteLine("\n--- 7) Reporting ---");

                var sellerSummary = service.GetSellerSalesSummary();
                foreach (var s in sellerSummary)
                {
                    Console.WriteLine($"Seller {s.SellerID}: {s.SellerName} - Revenue {s.TotalRevenue}");
                }

                var categorySummary = service.GetCategorySalesSummary();
                foreach (var c in categorySummary)
                {
                    Console.WriteLine($"Category {c.CategoryID}: {c.CategoryName} - Revenue {c.TotalRevenue}");
                }

                Console.WriteLine("\n=== SP BLL tests completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n*** TEST FAILED ***");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static int EnsureUserExists(IThriftHubService service, string email, string fullName, string role)
        {
            var existing = service.GetUserByEmail(email);
            if (existing != null)
            {
                Console.WriteLine($"User '{email}' already exists with ID {existing.UserID}");
                return existing.UserID;
            }

            var id = service.RegisterUser(new UserDto
            {
                FullName = fullName,
                Email = email,
                Password = "pass123",
                Role = role
            });

            Console.WriteLine($"Created user '{email}' with ID {id}");
            return id;
        }
    }
}