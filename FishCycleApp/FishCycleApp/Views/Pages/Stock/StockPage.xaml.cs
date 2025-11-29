using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Stock
{
    public partial class StockPage : Page
    {
        // ==========================================
        // STATICS / EVENTS
        // ==========================================
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        // ==========================================
        // FIELDS
        // ==========================================
        private readonly Person _currentUserProfile;
        private readonly ProductDataManager _dataManager = new ProductDataManager();

        private DataView? _productDataView;
        private bool _isLoading;
        private DateTime _lastSuccessUtc;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public StockPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += StockPage_Loaded;
            this.Unloaded += StockPage_Unloaded;
            this.IsVisibleChanged += StockPage_IsVisibleChanged;
        }

        // ==========================================
        // LIFECYCLE EVENTS
        // ==========================================
        private async void StockPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _productDataView == null)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                dgvStock.ItemsSource = _productDataView;
                UpdateResultInfo();
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

        // ==========================================
        // DATA LOADING
        // ==========================================
        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;

                // Load product data
                DataTable newTable = await _dataManager.LoadProductDataAsync();

                if (newTable != null)
                {
                    _productDataView = newTable.DefaultView;
                    dgvStock.ItemsSource = _productDataView;

                    _lastSuccessUtc = DateTime.UtcNow;
                    UpdateResultInfo();
                    ApplySearchFilter();
                }

                // Load statistics for cards
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StockPage] Error loading data: {ex.Message}");
                txtResultInfo.Text = "Error loading data. Please refresh.";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await _dataManager.GetStockStatisticsAsync();

                if (stats != null)
                {
                    // Update Card 1 - Total Jenis Ikan
                    var card1Text = this.FindName("txtTotalTypes") as TextBlock;
                    if (card1Text != null)
                        card1Text.Text = $"{stats.TotalProductTypes} Jenis";

                    // Update Card 2 - Total Stok
                    var card2Text = this.FindName("txtTotalStock") as TextBlock;
                    if (card2Text != null)
                        card2Text.Text = $"{stats.TotalStockQuantity:N2} kg";

                    // Update Card 3 - Nilai Total Stok
                    var card3Text = this.FindName("txtTotalValue") as TextBlock;
                    if (card3Text != null)
                        card3Text.Text = $"Rp{stats.TotalStockValue:N0}";

                    // Update Card 4 - Stok Rendah
                    var card4Text = this.FindName("txtLowStock") as TextBlock;
                    if (card4Text != null)
                        card4Text.Text = $"{stats.LowStockCount} Produk";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StockPage] Error loading statistics: {ex.Message}");
            }
        }

        private void UpdateResultInfo()
        {
            if (_productDataView != null)
            {
                int totalRecords = _productDataView.Count;
                string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
                txtResultInfo.Text = $"Total: {totalRecords} products found{suffix}";
            }
            else
            {
                txtResultInfo.Text = "No stock data available";
            }
        }

        // ==========================================
        // USER INTERACTIONS
        // ==========================================
        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddStockPage(_currentUserProfile));
        }

        // ==========================================
        // SEARCH & FILTER
        // ==========================================
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
            if (_productDataView == null) return;

            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...") searchText = string.Empty;

            string selectedGrade = (cmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();

            try
            {
                string filter = "";

                if (!string.IsNullOrEmpty(searchText))
                {
                    string search = searchText.Replace("'", "''");
                    filter = $"productid LIKE '%{search}%' OR product_name LIKE '%{search}%'";
                }

                if (!string.IsNullOrEmpty(selectedGrade) && selectedGrade != "All Grades")
                {
                    string gradeFilter = $"grade = '{selectedGrade}'";
                    filter = string.IsNullOrEmpty(filter) ? gradeFilter : $"{filter} AND {gradeFilter}";
                }

                _productDataView.RowFilter = string.IsNullOrEmpty(filter) ? null : filter;
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Filter Error: {ex.Message}");
            }
        }

        // ==========================================
        // UI HELPERS
        // ==========================================
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

        // ==========================================
        // ACTION HANDLERS
        // ==========================================
        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string productID = row["productid"].ToString();
                this.NavigationService.Navigate(new ViewStockPage(productID, _currentUserProfile));
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                var product = new Product
                {
                    ProductID = row["productid"].ToString(),
                    ProductName = row["product_name"].ToString(),
                    Grade = row["grade"].ToString(),
                    Quantity = Convert.ToDecimal(row["quantity"]),
                    UnitPrice = Convert.ToDecimal(row["unit_price"]),
                    SupplierID = row["supplierid"] != DBNull.Value ? row["supplierid"].ToString() : null
                };
                this.NavigationService.Navigate(new EditStockPage(product, _currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string productID = row["productid"].ToString();
                string productName = row["product_name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show(
                    $"Are you sure you want to delete product {productName}?",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    try
                    {
                        int result = await _dataManager.DeleteProductAsync(productID);

                        if (result != 0)
                        {
                            MessageBox.Show("Product deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                            NotifyDataChanged();
                            await LoadDataAsync();
                        }
                        else
                        {
                            var exists = await _dataManager.GetProductByIDAsync(productID);
                            if (exists == null)
                            {
                                await LoadDataAsync();
                            }
                            else
                            {
                                MessageBox.Show("Failed to delete product.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting product: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}