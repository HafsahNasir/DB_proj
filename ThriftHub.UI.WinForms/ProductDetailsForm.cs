using System;
using System.Windows.Forms;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.Domain.Models;

namespace ThriftHub.UI.WinForms
{
    public partial class ProductDetailsForm : Form
    {
        private readonly int _productId;
        private readonly IThriftHubService _service;
        private readonly UserDto _currentUser;

        public ProductDetailsForm(int productId, IThriftHubService service, UserDto currentUser)
        {
            InitializeComponent();
            _productId = productId;
            _service = service;
            _currentUser = currentUser;
        }

        private void ProductDetailsForm_Load(object sender, EventArgs e)
        {
            LoadProduct();
        }

        private void LoadProduct()
        {
            var product = _service.GetProductById(_productId);
            if (product == null)
            {
                MessageBox.Show("Product not found.");
                this.Close();
                return;
            }

            lblTitle.Text = product.Title;
            lblPrice.Text = $"Rs {product.Price}";
            lblCondition.Text = product.Condition;
            lblSeller.Text = product.SellerName;
            lblCategory.Text = product.CategoryName;
            lblStatus.Text = product.Status;

            // Description text
            txtDescription.Text = product.Description ?? "No description provided.";
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBuyNow_Click(object sender, EventArgs e)
        {
            try
            {
                // Buyer is the logged-in user
                int buyerId = _currentUser.UserID;

                // Calls BLL → which calls EF/SP → SQL stored procedure
                int orderId = _service.PlaceSingleProductOrder(
                    buyerId,
                    _productId,
                    "COD"   // temporary payment method
                );

                if (orderId > 0)
                {
                    MessageBox.Show($"Order placed successfully! Order ID: {orderId}");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Unable to place order. It may already be sold.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Order error:\n" + ex.Message);
            }
        }
    }
}