using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FishCycleApp
{
    public partial class ViewEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private string CurrentEmployeeID;

        public ViewEmployeePage(string employeeID)
        {
            InitializeComponent();
            this.CurrentEmployeeID = employeeID;
            LoadEmployeeDetails(employeeID);
        }

        // Fungsi untuk memuat detail employee berdasarkan ID
        private void LoadEmployeeDetails(string employeeID)
        {
            Employee employee = dataManager.GetEmployeeByID(employeeID);

            if (employee != null)
            {
                lblEmployeeID.Text = employee.EmployeeID;
                lblEmployeeName.Text = employee.EmployeeName;
                lblGoogleAccount.Text = employee.GoogleAccount;
            }
            else
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fungsi untuk mengedit employee
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new EditEmployeePage(CurrentEmployeeID));
        }

        // Fungsi untuk kembali ke halaman sebelumnya
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        // Fungsi untuk menghapus employee
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
    }
}
