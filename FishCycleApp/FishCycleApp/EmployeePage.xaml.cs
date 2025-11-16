using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class EmployeePage : Page
    {
        private Person currentUserProfile; // TAMBAHKAN INI
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private DataView EmployeeDataView;
        private DataTable fullEmployeeTable;

        public EmployeePage(Person userProfile)
        {
            InitializeComponent();
            this.currentUserProfile = userProfile; // SIMPAN USER PROFILE
            DisplayProfileData(userProfile);
            LoadData();
        }

        // Fungsi untuk Load Data Employee
        private void LoadData()
        {
            try
            {
                fullEmployeeTable = dataManager.LoadEmployeeData();
                EmployeeDataView = fullEmployeeTable.DefaultView;

                dgvEmployees.ItemsSource = EmployeeDataView;
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateResultInfo()
        {
            if (EmployeeDataView != null)
            {
                int totalRecords = EmployeeDataView.Count;
                int displayedRecords = Math.Min(10, totalRecords);
                txtResultInfo.Text = $"showing 1-{displayedRecords} result from {totalRecords} results";
            }
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search anything...";
                txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        // Fungsi untuk Menyaring Data berdasarkan Teks Pencarian
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmployeeDataView == null) return;

            string searchText = txtSearch.Text.Trim();

            if (searchText == "Search anything...")
                searchText = "";

            try
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    EmployeeDataView.RowFilter = null;
                }
                else
                {
                    string search = searchText.Replace("'", "''"); // Escape single quotes

                    string filterExpression = $"employee_id LIKE '%{search}%' OR " +
                                              $"name LIKE '%{search}%' OR " +
                                              $"google_account LIKE '%{search}%'";

                    EmployeeDataView.RowFilter = filterExpression;
                }

                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fungsi untuk Refresh Data Grid
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
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
                this.NavigationService.Navigate(new ViewEmployeePage(employeeID, this.currentUserProfile));
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Fungsi untuk Add Employee
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // PASS currentUserProfile ke AddEmployeePage
            this.NavigationService.Navigate(new AddEmployeePage(this.currentUserProfile));
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

        private void DisplayProfileData(Person profile)
        {
            if (profile.Names != null && profile.Names.Count > 0)
            {
                lblUserName.Text = profile.Names[0].DisplayName;
            }
            else
            {
                lblUserName.Text = "Pengguna Tidak Dikenal";
            }

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                string photoUrl = profile.Photos[0].Url;

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.EndInit();

                    imgUserProfile.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memuat foto profil: {ex.Message}", "Error Foto", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}