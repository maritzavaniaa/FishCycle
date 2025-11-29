using FishCycleApp.DataAccess;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class TransactionPage : Page
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
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();

        private DataView? _transactionDataView;
        private bool _isLoading;
        private DateTime _lastSuccessUtc;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public TransactionPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += TransactionPage_Loaded;
            this.Unloaded += TransactionPage_Unloaded;
            this.IsVisibleChanged += TransactionPage_IsVisibleChanged;
        }

        // ==========================================
        // LIFECYCLE EVENTS
        // ==========================================
        private async void TransactionPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _transactionDataView == null)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                dgvTransactions.ItemsSource = _transactionDataView;
                UpdateResultInfo();
            }
        }

        private void TransactionPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void TransactionPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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

                // Load transaction data from database
                DataTable newTable = await _dataManager.LoadTransactionDataAsync();

                if (newTable != null)
                {
                    _transactionDataView = newTable.DefaultView;
                    dgvTransactions.ItemsSource = _transactionDataView;

                    _lastSuccessUtc = DateTime.UtcNow;
                    UpdateResultInfo();
                    ApplySearchFilter();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransactionPage] Error loading data: {ex.Message}");
                txtResultInfo.Text = "Error loading data. Please refresh.";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo()
        {
            if (_transactionDataView != null)
            {
                int totalRecords = _transactionDataView.Count;
                string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
                txtResultInfo.Text = $"Total: {totalRecords} transactions found{suffix}";
            }
            else
            {
                txtResultInfo.Text = "No transaction data available";
            }
        }

        // ==========================================
        // USER INTERACTIONS
        // ==========================================
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddTransactionPage(_currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string transactionNumber = row["transaction_number"].ToString();
                this.NavigationService.Navigate(new ViewTransactionPage(transactionNumber, _currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string transactionNumber = row["transaction_number"].ToString();
                string clientName = row["client_name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show(
                    $"Are you sure you want to delete transaction {transactionNumber}?\nClient: {clientName}",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    try
                    {
                        int result = await _dataManager.DeleteTransactionAsync(transactionNumber);

                        if (result != 0)
                        {
                            MessageBox.Show("Transaction deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                            NotifyDataChanged();
                            await LoadDataAsync();
                        }
                        else
                        {
                            var exists = await _dataManager.GetTransactionByIDAsync(transactionNumber);
                            if (exists == null)
                            {
                                await LoadDataAsync();
                            }
                            else
                            {
                                MessageBox.Show("Failed to delete transaction.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting transaction: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void dgvTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection change if needed
        }

        private void ApplySearchFilter()
        {
            if (_transactionDataView == null) return;

            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...") searchText = string.Empty;

            try
            {
                string filter = "";

                if (!string.IsNullOrEmpty(searchText))
                {
                    string search = searchText.Replace("'", "''");
                    filter = $"transaction_number LIKE '%{search}%' OR client_name LIKE '%{search}%' OR employee_name LIKE '%{search}%'";
                }

                _transactionDataView.RowFilter = string.IsNullOrEmpty(filter) ? null : filter;
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
    }
}