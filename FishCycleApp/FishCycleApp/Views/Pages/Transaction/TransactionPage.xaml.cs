using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class TransactionPage : Page
    {
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person _currentUserProfile;
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly ClientDataManager _clientManager = new ClientDataManager();
        private readonly EmployeeDataManager _employeeManager = new EmployeeDataManager();

        private List<TransactionModel> _allTransactions = new List<TransactionModel>(); private bool _isLoading;
        private DateTime _lastSuccessUtc;

        public TransactionPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += TransactionPage_Loaded;
            this.Unloaded += TransactionPage_Unloaded;
            this.IsVisibleChanged += TransactionPage_IsVisibleChanged;
        }

        private async void TransactionPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _allTransactions.Count == 0)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                ApplySearchFilter();
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

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            try
            {
                _isLoading = true;

                var taskTrans = _dataManager.LoadTransactionDataAsync();
                var taskClients = _clientManager.LoadClientDataAsync();
                var taskEmps = _employeeManager.LoadEmployeeDataAsync();

                await Task.WhenAll(taskTrans, taskClients, taskEmps);

                var transactions = taskTrans.Result;
                var clients = taskClients.Result;
                var employees = taskEmps.Result;

                if (transactions != null)
                {
                    foreach (var t in transactions)
                    {
                        var client = clients.FirstOrDefault(c => c.ClientID == t.ClientID);
                        t.ClientName = client?.ClientName ?? "Unknown Client";

                        var emp = employees.FirstOrDefault(e => e.EmployeeID == t.AdminID);
                        t.EmployeeName = emp?.EmployeeName ?? "Unknown Employee";
                    }

                    _allTransactions = transactions.OrderByDescending(t => t.TransactionDate).ToList();
                    _lastSuccessUtc = DateTime.UtcNow;

                    ApplySearchFilter();
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

        private void UpdateResultInfo(int count)
        {
            string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
            if (txtResultInfo != null)
                txtResultInfo.Text = $"Total: {count} transactions found{suffix}";
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddTransactionPage(_currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TransactionModel t)
            {
                this.NavigationService.Navigate(new ViewTransactionPage(t.TransactionID, _currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TransactionModel t)
            {
                var confirm = MessageBox.Show($"Delete {t.TransactionID}?", "CONFIRM", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                await _dataManager.DeleteTransactionAsync(t.TransactionID);

                MessageBox.Show("Deleted!");
                NotifyDataChanged();
                await LoadDataAsync();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TransactionModel t)
            {
                this.NavigationService.Navigate(new EditTransactionPage(t, _currentUserProfile));
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed!", "SUCCESS");
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

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplySearchFilter();

        private void dgvTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ApplySearchFilter()
        {
            if (dgvTransactions == null || _allTransactions == null || _allTransactions.Count == 0) return;

            IEnumerable<TransactionModel> query = _allTransactions;

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "search anything...")
            {
                query = query.Where(t =>
                    (t.TransactionID != null && t.TransactionID.ToLower().Contains(searchText)) ||
                    (t.ClientName != null && t.ClientName.ToLower().Contains(searchText)) ||
                    (t.EmployeeName != null && t.EmployeeName.ToLower().Contains(searchText))
                );
            }

            if (cmbDateRange.SelectedItem is ComboBoxItem dateItem && dateItem.Tag != null)
            {
                string dateFilter = dateItem.Tag.ToString(); 
                DateTime today = DateTime.Now.Date;

                switch (dateFilter)
                {
                    case "Today":
                        query = query.Where(t => t.TransactionDate.ToLocalTime().Date == today);
                        break;
                    case "Week":
                        query = query.Where(t => t.TransactionDate.ToLocalTime().Date >= today.AddDays(-7));
                        break;
                    case "Month":
                        query = query.Where(t => t.TransactionDate.ToLocalTime().Date >= today.AddDays(-30));
                        break;
                    default: 
                        break;
                }
            }

            if (cmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Tag != null)
            {
                string statusFilter = statusItem.Tag.ToString(); 

                if (statusFilter != "All")
                {
                    query = query.Where(t =>
                        !string.IsNullOrEmpty(t.PaymentStatus) &&
                        t.PaymentStatus.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            var resultList = query.OrderByDescending(t => t.TransactionDate).ToList();

            dgvTransactions.ItemsSource = resultList;

            UpdateResultInfo(resultList.Count);
        }

        private void DisplayProfileData(Person profile)
        {
            
            if (lblUserName != null) lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "User";
            if (imgUserProfile != null && profile?.Photos?.Count > 0)
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
                catch { }
            }
        }

        private void cmbDateRange_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySearchFilter();

        private void cmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySearchFilter();
    }
}