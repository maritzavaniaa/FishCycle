using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FishCycleApp
{
    public partial class SupplierPage : Page
    {
        // Reuse global reload pattern like EmployeePage
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;
        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person currentUserProfile;
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();

        private DataView? SupplierDataView;
        private DataTable? fullSupplierTable;
        private DataTable? lastSuccessfulTable;
        private bool _isLoading;
        private DateTime _lastSuccessUtc;
        private bool _firstVisibilityHandled;

        public SupplierPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += SupplierPage_Loaded;
            this.Unloaded += SupplierPage_Unloaded;
            this.IsVisibleChanged += SupplierPage_IsVisibleChanged;

            _ = EnsureInitialLoadAsync();
        }

        private void SupplierPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload)
            {
                PendingReload = false;
                LoadData();
                return;
            }

            if (lastSuccessfulTable != null)
            {
                SupplierDataView = lastSuccessfulTable.DefaultView;
                dgvSuppliers.ItemsSource = SupplierDataView;
                UpdateResultInfo();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(async () => await TryLoadWithRetryAsync(2, 200)),
                                       DispatcherPriority.Background);
            }
        }

        private void SupplierPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private void SupplierPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                if (!_firstVisibilityHandled)
                {
                    _firstVisibilityHandled = true;
                    return;
                }

                if (PendingReload)
                {
                    PendingReload = false;
                    LoadData();
                }
            }
        }

        private void OnGlobalReloadRequested()
        {
            Dispatcher.Invoke(async () => await TryLoadWithRetryAsync(2, 150));
        }

        private async Task EnsureInitialLoadAsync()
        {
            await Dispatcher.Yield(DispatcherPriority.Loaded);
            await TryLoadWithRetryAsync(3, 250);
        }

        private async Task TryLoadWithRetryAsync(int maxAttempts, int delayMs)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                LoadData();

                if (SupplierDataView != null)
                    break;

                await Task.Delay(delayMs);
            }
        }

        private void LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var newTable = supplierManager.LoadSupplierData();
                fullSupplierTable = newTable;

                if (newTable != null && newTable.Rows.Count > 0)
                {
                    lastSuccessfulTable = newTable.Copy();
                    _lastSuccessUtc = DateTime.UtcNow;

                    SupplierDataView = newTable.DefaultView;
                    dgvSuppliers.ItemsSource = SupplierDataView;
                    UpdateResultInfo();
                }
                else
                {
                    if (lastSuccessfulTable != null)
                    {
                        SupplierDataView = lastSuccessfulTable.DefaultView;
                        dgvSuppliers.ItemsSource = SupplierDataView;
                        UpdateResultInfo();
                        Console.WriteLine("Load returned empty; keeping last successful supplier data.");
                    }
                    else
                    {
                        SupplierDataView = null;
                        dgvSuppliers.ItemsSource = null;
                        txtResultInfo.Text = "No supplier data available";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading supplier data: {ex.Message}");

                if (lastSuccessfulTable != null)
                {
                    SupplierDataView = lastSuccessfulTable.DefaultView;
                    dgvSuppliers.ItemsSource = SupplierDataView;
                    UpdateResultInfo();
                }
                else
                {
                    txtResultInfo.Text = "Error loading data. Please refresh.";
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo()
        {
            if (SupplierDataView != null)
            {
                int totalRecords = SupplierDataView.Count;
                int displayedRecords = Math.Min(10, totalRecords);
                string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
                txtResultInfo.Text = $"showing 1-{displayedRecords} result from {totalRecords} results{suffix}";
            }
            else
            {
                txtResultInfo.Text = "No supplier data available";
            }
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
            {
                txtSearch.Text = string.Empty;
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
            if (searchText == "Search anything...") searchText = string.Empty;
            ApplyCombinedFilter(searchText);
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...") searchText = string.Empty;
            ApplyCombinedFilter(searchText);
        }

        private void ApplyCombinedFilter(string searchText)
        {
            if (SupplierDataView == null) return;

            try
            {
                var filters = new List<string>();

                // Search filter across supplier columns
                if (!string.IsNullOrEmpty(searchText))
                {
                    string search = searchText.Replace("'", "''");
                    filters.Add($"(supplierid LIKE '%{search}%' OR " +
                                $"supplier_type LIKE '%{search}%' OR " +
                                $"supplier_name LIKE '%{search}%' OR " +
                                $"supplier_phone LIKE '%{search}%' OR " +
                                $"supplier_address LIKE '%{search}%')");
                }

                // Category filter (supplier_type)
                var selectedCategory = (cmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(selectedCategory) &&
                    selectedCategory != "Category" &&
                    selectedCategory != "All Categories")
                {
                    string cat = selectedCategory.Replace("'", "''");
                    filters.Add($"supplier_type = '{cat}'");
                }

                SupplierDataView.RowFilter = filters.Count > 0 ? string.Join(" AND ", filters) : null;
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
            if (sender is Button button && button.DataContext is DataRowView selectedRow)
            {
                string supplierID = selectedRow["supplierid"].ToString();
                this.NavigationService.Navigate(new ViewSupplierPage(supplierID, currentUserProfile));
            }
            else
            {
                MessageBox.Show("Unable to retrieve supplier details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddSupplierPage(currentUserProfile));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string id = row["supplierid"].ToString();
                string name = row["supplier_name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show(
                    $"Are you sure you want to delete Supplier '{name}'?",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    int result = supplierManager.DeleteSupplier(id);

                    bool success = result != 0;
                    if (!success)
                    {
                        var stillThere = supplierManager.GetSupplierByID(id);
                        success = (stillThere == null);
                    }

                    if (success)
                    {
                        MessageBox.Show("Supplier deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                        NotifyDataChanged();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadData();
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to retrieve supplier details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                ? profile.Names[0].DisplayName
                : "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                string photoUrl = profile.Photos[0].Url;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load profile photo: {ex.Message}");
                }
            }
        }
    }
}