using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Stock
{
    public partial class StockPage : Page
    {
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person _currentUserProfile;
        private readonly ProductDataManager _dataManager = new ProductDataManager();
        private readonly SupplierDataManager _supplierManager = new SupplierDataManager();

        private List<Product> _allProducts = new List<Product>(); private bool _isLoading;
        private DateTime _lastSuccessUtc;

        public StockPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += StockPage_Loaded;
            this.Unloaded += StockPage_Unloaded;
            this.IsVisibleChanged += StockPage_IsVisibleChanged;
        }

        private async void StockPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _allProducts.Count == 0)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                ApplySearchFilter();
            }
        }

        private void StockPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void StockPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
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

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            try
            {
                _isLoading = true;

                var taskProducts = _dataManager.LoadProductDataAsync();
                var taskSuppliers = _supplierManager.LoadSupplierDataAsync();

                await Task.WhenAll(taskProducts, taskSuppliers);

                var products = taskProducts.Result;
                var suppliers = taskSuppliers.Result;

                if (products != null)
                {
                    foreach (var prod in products)
                    {
                        var match = suppliers.FirstOrDefault(s => s.SupplierID == prod.SupplierID);
                        if (match != null)
                        {
                            prod.SupplierName = match.SupplierName;
                        }
                    }

                    _allProducts = products;
                    _lastSuccessUtc = DateTime.UtcNow;

                    ApplySearchFilter();

                    var stats = _dataManager.CalculateStatistics(_allProducts);
                    UpdateStatisticsUI(stats);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateStatisticsUI(StockStatistics stats)
        {
            if (txtTotalTypes != null)
                txtTotalTypes.Text = $"{stats.TotalProductTypes} Jenis";

            if (txtTotalStock != null)
                txtTotalStock.Text = $"{stats.TotalStockQuantity:N2} kg";

            if (txtTotalValue != null)
                txtTotalValue.Text = $"Rp{stats.TotalStockValue:N0}";

            if (txtLowStock != null)
                txtLowStock.Text = $"{stats.LowStockCount} Produk";
        }

        private void UpdateResultInfo(int count)
        {
            string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
            txtResultInfo.Text = $"Total: {count} products found{suffix}";
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddStockPage(_currentUserProfile));
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new SolidColorBrush(Colors.Black);
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
            ApplySearchFilter();
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (dgvStock == null || _allProducts == null) return;

            IEnumerable<Product> query = _allProducts;

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "search anything...")
            {
                query = query.Where(p =>
                    (p.ProductID != null && p.ProductID.ToLower().Contains(searchText)) ||
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchText))
                );
            }

            string selectedGrade = (cmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Grades";
            if (selectedGrade != "All Grades" && !string.IsNullOrEmpty(selectedGrade))
            {
                query = query.Where(p => p.Grade == selectedGrade);
            }

            var resultList = query.ToList();
            dgvStock.ItemsSource = resultList;
            UpdateResultInfo(resultList.Count);
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                ? profile.Names[0].DisplayName
                : "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(profile.Photos[0].Url, UriKind.Absolute);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch { /* Ignore */ }
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product p)
            {
                this.NavigationService.Navigate(new ViewStockPage(p.ProductID, _currentUserProfile));
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product p)
            {
                this.NavigationService.Navigate(new EditStockPage(p, _currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product p)
            {
                var confirm = MessageBox.Show($"Delete product {p.ProductName}?", "CONFIRM", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                await _dataManager.DeleteProductAsync(p.ProductID);

                MessageBox.Show("Deleted!");
                NotifyDataChanged();
                await LoadDataAsync();
            }
        }
    }
}