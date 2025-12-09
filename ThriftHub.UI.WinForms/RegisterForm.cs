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
    public partial class RegisterForm : Form
    {
        private readonly IThriftHubService _service;
        public UserDto RegisteredUser { get; private set; }

        public RegisterForm(IThriftHubService service)
        {
            InitializeComponent();
            _service = service;
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {

        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirm = txtConfirmPassword.Text;
            string phone = txtPhone.Text.Trim();
            string address = txtAddress.Text.Trim();

            // Validation
            if (string.IsNullOrWhiteSpace(fullName)) { MessageBox.Show("Full name is required."); return; }
            if (string.IsNullOrWhiteSpace(email)) { MessageBox.Show("Email is required."); return; }
            if (string.IsNullOrWhiteSpace(password)) { MessageBox.Show("Password is required."); return; }
            if (password != confirm) { MessageBox.Show("Passwords do not match."); return; }

            try
            {
                // Check if email already exists
                var existing = _service.GetUserByEmail(email);
                if (existing != null)
                {
                    MessageBox.Show("An account with this email already exists.");
                    return;
                }

                var dto = new UserDto
                {
                    FullName = fullName,
                    Email = email,
                    Password = password,
                    Phone = phone,
                    Address = address,

                    //Always assign BuyerSeller (as sab ka role will be same)
                    Role = "BuyerSeller"
                };

                dto.UserID = _service.RegisterUser(dto);
                RegisteredUser = dto;

                MessageBox.Show("Registration successful!", "Success");

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registration error:\n" + ex.Message);
            }
        }
    }
}
