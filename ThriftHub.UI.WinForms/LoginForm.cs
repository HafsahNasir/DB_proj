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
    public partial class LoginForm : Form
    {
        private readonly IThriftHubService _service;
        private UserDto _currentUser;  // logged in user
                                       
        public LoginForm() : this(null)
        {
        }

        // New constructor that takes the service from Program.cs
        public LoginForm(IThriftHubService service)
        {
            _service = service;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter an email.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter a password.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_service == null)
            {
                MessageBox.Show("Service is not initialized.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // try to find the user by email in current users
                var user = _service.GetUserByEmail(email);

                if (user == null)
                {
                    // if u dont find then we ask to register
                    var result = MessageBox.Show(
                        "User not found. Register this email as a new Buyer/Seller?",
                        "Register",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                        return;

                    var newUser = new UserDto
                    {
                        FullName = email,
                        Email = email,
                        Password = password,  // store entered password
                        Role = "BuyerSeller"
                    };

                    int newId = _service.RegisterUser(newUser);
                    newUser.UserID = newId;
                    user = newUser;
                }
                else
                {
                    // if found then we check pass
                    if (user.Password != password)
                    {
                        MessageBox.Show("Incorrect password.", "Login failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // if it works then we set as current user
                _currentUser = user;
                lblCurrentUser.Text = $"Logged in as: {user.FullName} ({user.Email})";

                //Go to DashboardForm after successful login
                var dashboard = new DashboardForm(_service, user);
                dashboard.Show();
                this.Hide(); // hide login form so user cannot go back

            }
            catch (Exception ex)
            {
                MessageBox.Show("Login/Register error:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRegister_Click_1(object sender, EventArgs e)
        {
            var reg = new RegisterForm(_service);

            if (reg.ShowDialog() == DialogResult.OK)
            {
                // Automatically log in the new user
                _currentUser = reg.RegisteredUser;

                var dashboard = new DashboardForm(_service, _currentUser);
                dashboard.Show();
                this.Hide();
            }
        }
    }
}
