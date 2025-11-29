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

namespace FishCycleApp
{
    public partial class SupplierPage : Page
    {
        // ============================================================
        // GLOBAL RELOAD MECHANISM (IDENTIK DENGAN EMPLOYEE PAGE)
        // ============================================================
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        // ============================================================
        // FIELDS
        // ============================================================
        private readonly Person currentUserProfile;
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();

        private DataView? SupplierDataView;
        private DateTime _lastSuccessUtc;
        private bool _isLoading;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        public SupplierPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);

            this.Loaded += SupplierPage_Loaded;
            this.Unloaded += SupplierPage_Unloaded;
            this.IsVisibleChanged += SupplierPage_IsVisibleChanged;
        }

        // ============================================================
        // PAGE LIFECYCLE (IDENTIK DENGAN EMPLOYEE PAGE)
        // ============================================================
        private async void SupplierPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            // Jika pertama kali load, atau data belum ada, atau habis update/delete/add
            if (PendingReload || SupplierDataView == null)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                dgvSuppliers.ItemsSource = SupplierDataView;
                UpdateResultInfo();
            }
        }

        private void SupplierPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void SupplierPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Silent reload saat user kembali dari View/Edit
            if (IsVisible && PendingReload)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
        }

        private async void OnGlobalReloadRequested()
        {
            await Dispatcher.InvokeAsync(async () => await LoadDataAsync());
        }

        // ============================================================
        // LOAD DATA (PERSIS EMPLOYEE PAGE)
        // ============================================================
        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;

                DataTable dt = await supplierManager.LoadSupplierDataAsync();

                SupplierDataView = dt.DefaultView;
                dgvSuppliers.ItemsSource = SupplierDataView;

                _lastSuccessUtc = DateTime.UtcNow;
                UpdateResultInfo();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                txtResultInfo.Text = "Error loading supplier data.";
                Console.WriteLine($"[SupplierPage] Load error: {ex.Message}");
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
                int total = SupplierDataView.Count;
                string time = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : "";

                txtResultInfo.Text = $"Total: {total} suppliers found{time}";
            }
            else
            {
                txtResultInfo.Text = "No supplier data available";
            }
        }

        // ============================================================
        // SEARCH & FILTER
        // ============================================================
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (SupplierDataView == null) return;

            List<string> filters = new List<string>();

            // text search
            string search = txtSearch.Text.Trim();
            if (search == "Search anything...") search = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Replace("'", "''");
                filters.Add(
                    $"supplierid LIKE '%{s}%' OR " +
                    $"supplier_type LIKE '%{s}%' OR " +
                    $"supplier_name LIKE '%{s}%' OR " +
                    $"supplier_phone LIKE '%{s}%' OR " +
                    $"supplier_address LIKE '%{s}%'"
                );
            }

            // category filter
            if (cmbCategory.SelectedItem is ComboBoxItem item)
            {
                string cat = item.Content.ToString();
                if (cat != "All Categories" && cat != "Category")
                {
                    string esc = cat.Replace("'", "''");
                    filters.Add($"supplier_type = '{esc}'");
                }
            }

            SupplierDataView.RowFilter = filters.Count > 0
                ? string.Join(" AND ", filters)
                : "";

            UpdateResultInfo();
        }

        // ============================================================
        // BUTTON HANDLERS
        // ============================================================
        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddSupplierPage(currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                string id = row["supplierid"].ToString();
                this.NavigationService.Navigate(new ViewSupplierPage(id, currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                string id = row["supplierid"].ToString();
                string name = row["supplier_name"].ToString();

                var confirm = MessageBox.Show(
                    $"Are you sure you want to delete Supplier \"{name}\"?",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                // Delete supplier
                await supplierManager.DeleteSupplierAsync(id);

                // VALID WAY → cek apakah sudah tidak ada di database
                var stillExists = await supplierManager.GetSupplierByIDAsync(id);
                bool success = stillExists == null;

                if (success)
                {
                    MessageBox.Show("Supplier deleted successfully.",
                        "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    NotifyDataChanged();
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.",
                        "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============================================================
        // PROFILE DISPLAY
        // ============================================================
        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "User";

            try
            {
                if (profile?.Photos?.Count > 0)
                {
                    var bm = new BitmapImage();
                    bm.BeginInit();
                    bm.CacheOption = BitmapCacheOption.OnLoad;
                    bm.UriSource = new Uri(profile.Photos[0].Url);
                    bm.EndInit();
                    imgUserProfile.Source = bm;
                }
            }
            catch { }
        }

        // search placeholder
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
                txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Search anything...";
        }
    }
}
