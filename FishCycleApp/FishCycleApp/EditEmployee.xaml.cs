using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace FishCycleApp
{
    public partial class EditEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private string CurrentEmployeeID;

        public EditEmployeePage(string employeeID)
        {
            InitializeComponent();
            this.CurrentEmployeeID = employeeID;
            LoadEmployeeDetails(employeeID);
            txtEmployeeID.IsReadOnly = true;
        }

        // Fungsi untuk memuat detail employee berdasarkan ID
        private void LoadEmployeeDetails(string employeeID)
        {
            Employee employee = dataManager.GetEmployeeByID(employeeID);

            if (employee != null)
            {
                txtEmployeeID.Text = employee.EmployeeID;
                txtEmployeeName.Text = employee.EmployeeName;
                txtGoogleAccount.Text = employee.GoogleAccount;
            }
            else
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        // Fungsi untuk menyimpan perubahan data employee
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text) || string.IsNullOrWhiteSpace(txtGoogleAccount.Text))
            {
                MessageBox.Show("Please fill in all fields.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Employee updatedEmployee = new Employee
            {
                EmployeeID = CurrentEmployeeID,
                EmployeeName = txtEmployeeName.Text,
                GoogleAccount = txtGoogleAccount.Text
            };

            int result = dataManager.UpdateEmployee(updatedEmployee);

            if (result == 1)
            {
                MessageBox.Show("Employee updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                this.NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fungsi untuk menghapus data employee
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show($"Are you sure you want to delete this employee? Employee ID: {CurrentEmployeeID}", "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = dataManager.DeleteEmployee(CurrentEmployeeID);

                if (result == 1)
                {
                    MessageBox.Show("Employee deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Fungsi untuk kembali ke halaman sebelumnya
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}
