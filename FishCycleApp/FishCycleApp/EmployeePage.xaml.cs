using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;

namespace FishCycleApp
{
    public partial class EmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private DataView EmployeeDataView;

        public EmployeePage()
        {
            InitializeComponent();
            LoadData();
        }

        // Fungsi untuk Load Data Employee
        private void LoadData()
        {
            try
            {
                DataTable employeeTable = dataManager.LoadEmployeeData();

                EmployeeDataView = employeeTable.DefaultView;

                dgvEmployees.ItemsSource = EmployeeDataView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fungsi untuk Refresh Data Grid
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // Fungsi untuk View Employee Detail
        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string employeeID = selectedRow["employee_id"].ToString();

                // Navigasi ke halaman ViewEmployeePage
                this.NavigationService.Navigate(new ViewEmployeePage(employeeID));
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Fungsi untuk Edit Employee
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string employeeID = selectedRow["employee_id"].ToString();

                // Navigasi ke halaman EditEmployeePage
                this.NavigationService.Navigate(new EditEmployeePage(employeeID));
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.",
                                "ERROR",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }

        // Fungsi untuk Add Employee
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Navigasi ke halaman AddEmployeePage
            this.NavigationService.Navigate(new AddEmployeePage());
        }

        // Fungsi untuk Delete Employee
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string employeeID = selectedRow["employee_id"].ToString();
                string employeeName = selectedRow["name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show($"Are you sure you want to delete Employee Name {employeeName}?", "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    int result = dataManager.DeleteEmployee(employeeID);

                    if (result == 1)
                    {
                        MessageBox.Show("Employee deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Fungsi untuk Menangani Perubahan Tampilan Halaman
        private void EmployeePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible == true)
            {
                LoadData();
            }
        }

        // Fungsi untuk Menyaring Data berdasarkan Teks Pencarian
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmployeeDataView == null) return;

            string searchText = (sender as TextBox).Text.Trim();
            string filterExpression = "";

            if (string.IsNullOrEmpty(searchText))
            {
                EmployeeDataView.RowFilter = null;
            }
            else
            {
                string search = $"%{searchText}%";

                filterExpression = $"employee_id LIKE '{search}' OR " +
                                   $"employee_name LIKE '{search}' OR " +
                                   $"google_account LIKE '{search}'";

                EmployeeDataView.RowFilter = filterExpression;
            }
        }
    }
}
