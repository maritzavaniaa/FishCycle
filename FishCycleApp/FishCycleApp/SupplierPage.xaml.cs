using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class SupplierPage : Page
    {
        private Person currentUserProfile;
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private DataView SupplierDataView = null;
        private DataTable fullSupplierTable;

        public SupplierPage(Person userProfile)
        {
            InitializeComponent();
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                fullSupplierTable = supplierManager.LoadSupplierData();
                SupplierDataView = fullSupplierTable.DefaultView;

                dgvSuppliers.ItemsSource = SupplierDataView;
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading client data: " + ex.Message, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateResultInfo()
        {
            if (SupplierDataView != null)
            {
                int totalRecords = SupplierDataView.Count;
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

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (searchText == "Search anything...")
                searchText = "";

            ApplyCombinedFilter(searchText);
        }

        // Category filter
        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...")
                searchText = "";

            ApplyCombinedFilter(searchText);
        }

        private void ApplyCombinedFilter(string searchText)
        {
            if (SupplierDataView == null) return;

            try
            {
                string filterExpression = "";
                List<string> filters = new List<string>();

                // Search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    string search = searchText.Replace("'", "''"); // Escape single quotes
                    filters.Add($"(suppliertid LIKE '%{search}%' OR " +
                               $"supplier_name LIKE '%{search}%' OR " +
                               $"supplier_phone LIKE '%{search}%' OR " +
                               $"supplier_address LIKE '%{search}%')");
                }

                // Category filter
                var selectedCategory = (cmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(selectedCategory) &&
                    selectedCategory != "Category" &&
                    selectedCategory != "All Categories")
                {
                    filters.Add($"supplier_type = '{selectedCategory}'");
                }

                // Combine filters
                if (filters.Count > 0)
                {
                    filterExpression = string.Join(" AND ", filters);
                    SupplierDataView.RowFilter = filterExpression;
                }
                else
                {
                    SupplierDataView.RowFilter = null;
                }

                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string supplierID = selectedRow["supplierid"].ToString();
                this.NavigationService.Navigate(new ViewSupplierPage(supplierID, this.currentUserProfile));
            }
            else
            {
                MessageBox.Show("Unable to retrieve supplier details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddSupplierPage(this.currentUserProfile));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = (DataRowView)((Button)sender).DataContext;
            string id = row["supplierid"].ToString();

            if (MessageBox.Show("Delete this supplier?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                supplierManager.DeleteSupplier(id);
                LoadData();
            }
        }

        private void SupplierPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
