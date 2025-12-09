using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.Domain.Models;

namespace ThriftHub.UI.WinForms
{
    public partial class DashboardForm : Form
    {
        private readonly IThriftHubService _service;
        private readonly UserDto _currentUser;

        public DashboardForm(IThriftHubService service, UserDto user)
        {
            InitializeComponent();
            dgvProducts.AutoGenerateColumns = true;
            _service = service;
            _currentUser = user;

            lblWelcome.Text = $"Welcome, {_currentUser.FullName}";
            LoadConditionDropdown();
            // Don't load categories in constructor - do it in Load event
        }

        // Add Form Load event handler
        private void DashboardForm_Load(object sender, EventArgs e)
        {
            LoadCategoryDropdown();
        }

        // -------------------------------
        // LOAD CONDITION DROPDOWN
        // -------------------------------
        private void LoadConditionDropdown()
        {
            cboCondition.Items.Clear();
            cboCondition.Items.Add("New");
            cboCondition.Items.Add("Used");
            cboCondition.Items.Add("Very Good");
            cboCondition.Items.Add("Good");
            cboCondition.Items.Add("Fair");

            cboCondition.SelectedIndex = -1;
        }

        // -------------------------------
        // LOAD CATEGORY DROPDOWN
        // -------------------------------
        private void LoadCategoryDropdown()
        {
            try
            {
                // Show a loading message
                cboCategory.Items.Clear();
                cboCategory.Items.Add("Loading...");
                cboCategory.SelectedIndex = 0;
                cboCategory.Enabled = false;

                // Use BeginInvoke to allow UI to update first
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var categories = _service.GetAllCategories().ToList();

                        cboCategory.Items.Clear();
                        cboCategory.DataSource = categories;
                        cboCategory.DisplayMember = "CategoryName";
                        cboCategory.ValueMember = "CategoryID";
                        cboCategory.SelectedIndex = -1;
                        cboCategory.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        cboCategory.Items.Clear();
                        cboCategory.Enabled = true;
                        MessageBox.Show("Unable to load categories:\n" + ex.Message, "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to initialize category dropdown:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void lblWelcome_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                var criteria = new ProductSearchCriteria
                {
                    Keyword = txtKeyword.Text.Trim(),

                    MinPrice = decimal.TryParse(txtMinPrice.Text.Trim(), out decimal min)
                                ? min
                                : (decimal?)null,

                    MaxPrice = decimal.TryParse(txtMaxPrice.Text.Trim(), out decimal max)
                                ? max
                                : (decimal?)null,

                    Condition = cboCondition.SelectedIndex >= 0
                                ? cboCondition.SelectedItem.ToString()
                                : null,

                    CategoryID = cboCategory.SelectedIndex >= 0
                                ? (int?)cboCategory.SelectedValue
                                : null
                };

                var results = _service.SearchProducts(criteria).ToList();

                if (results.Count == 0)
                {
                    MessageBox.Show("No products found.");
                    dgvProducts.DataSource = null;
                    return;
                }

                dgvProducts.DataSource = results;

                // Hide columns you don't want
                if (dgvProducts.Columns.Contains("Description"))
                    dgvProducts.Columns["Description"].Visible = false;

                if (dgvProducts.Columns.Contains("ImageURL"))
                    dgvProducts.Columns["ImageURL"].Visible = false;

                if (dgvProducts.Columns.Contains("SellerID"))
                    dgvProducts.Columns["SellerID"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error:\n" + ex.Message);
            }
        }

        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var product = dgvProducts.Rows[e.RowIndex].DataBoundItem as ProductDto;
            if (product == null) return;

            var form = new ProductDetailsForm(product.ProductID, _service, _currentUser);
            form.ShowDialog();

            // Refresh search results after changes (ex: product purchased)
            btnSearch_Click(sender, EventArgs.Empty);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var product = dgvProducts.Rows[e.RowIndex].DataBoundItem as ProductDto;
            if (product == null)
                return;

            var form = new ProductDetailsForm(product.ProductID, _service, _currentUser);
            form.ShowDialog();
            
            // Refresh the product list after closing details form
            // (in case the product was purchased)
            btnSearch_Click(sender, e);
        }
    }
}
