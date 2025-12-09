using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThriftHub.Domain.Models;
using ThriftHub.BLL.Interfaces.Services;
using ThriftHub.BLL.EF;
using ThriftHub.BLL.SP;

namespace ThriftHub.UI.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Toggle this flag to test EF vs SP
                bool useEf = true; // true = EF, false = SP

                IThriftHubService service =
                    useEf ? (IThriftHubService)new ThriftHubEfService()
                          : new ThriftHubSpService();

                // Test database connection by trying to get user count (simple query)
                try
                {
                    // This will fail fast if DB connection is broken
                    var testUser = service.GetUserByEmail("test@connection.check");
                }
                catch (Exception dbEx)
                {
                    MessageBox.Show(
                        "Database connection failed!\n\n" +
                        "Please check:\n" +
                        "1. SQL Server is running\n" +
                        "2. Connection string in App.config is correct\n" +
                        "3. Database 'ThriftHubDB' exists\n\n" +
                        "Error: " + dbEx.Message,
                        "Database Connection Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Application.Run(new LoginForm(service));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Application startup failed:\n" + ex.Message,
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}

