using System;
using System.Windows;
using System.Windows.Controls;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;

namespace FishCycleApp
{
    public partial class AddEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();

        public AddEmployeePage()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input
            if (string.IsNullOrEmpty(txtEmployeeName.Text) || string.IsNullOrEmpty(txtGoogleAccount.Text))
            {
                MessageBox.Show("Please fill in all fields.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Membuat objek Employee
            Employee newEmployee = new Employee
            {
                EmployeeID = "EID-" + DateTime.Now.ToString("yyMMddHHmmss"), // Auto-generate ID
                EmployeeName = txtEmployeeName.Text,
                GoogleAccount = txtGoogleAccount.Text
            };

            // Menyimpan data Employee menggunakan data manager
            int result = dataManager.InsertEmployee(newEmployee);

            if (result == 1)
            {
                MessageBox.Show("Employee added successfully!",
                                "SUCCESS",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // Navigasi kembali jika bisa
                if (this.NavigationService?.CanGoBack == true)
                {
                    this.NavigationService.GoBack();
                }
            }
            else
            {
                MessageBox.Show("Failed to add employee.",
                                "ERROR",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Navigasi kembali jika tombol Cancel diklik
            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}
