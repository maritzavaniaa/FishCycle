using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class ViewTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly ClientDataManager _clientManager = new ClientDataManager();     
        private readonly EmployeeDataManager _employeeManager = new EmployeeDataManager();
        private readonly ProductDataManager _productManager = new ProductDataManager();

        private readonly Person _currentUserProfile;
        private TransactionModel? _loadedTransaction;
        private string _currentTransactionID;

        private CancellationTokenSource? _cts;

        public ViewTransactionPage(string transactionID, Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            _currentTransactionID = transactionID?.Trim() ?? "";

            DisplayProfileData(userProfile);

            this.Loaded += ViewTransactionPage_Loaded;
            this.Unloaded += ViewTransactionPage_Unloaded;
            this.IsVisibleChanged += ViewTransactionPage_IsVisibleChanged;
        }

        private void ViewTransactionPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDataSafe(isSilent: false);
        }

        private void ViewTransactionPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void ViewTransactionPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ReloadDataSafe(isSilent: true);
            }
        }

        private void ReloadDataSafe(bool isSilent)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ = LoadTransactionDetailsAsync(_currentTransactionID, isSilent, _cts.Token);
        }

        private async Task LoadTransactionDetailsAsync(string transactionID, bool isSilent, CancellationToken token)
        {
            try
            {
                if (!isSilent) this.Cursor = System.Windows.Input.Cursors.Wait;

                var result = await _dataManager.GetTransactionByIDAsync(transactionID, token);
                if (token.IsCancellationRequested) return;

                _loadedTransaction = result;

                if (_loadedTransaction != null)
                {
                    await PopulateRelatedDataAsync(_loadedTransaction);
                    ApplyToUI(_loadedTransaction);

                    var items = await _dataManager.GetTransactionItemsAsync(transactionID);

                    foreach (var item in items)
                    {
                        var productInfo = await _productManager.GetProductByIDAsync(item.ProductID);

                        if (productInfo != null)
                        {
                            item.ProductName = productInfo.ProductName;
                            item.ProductGrade = productInfo.Grade;
                        }
                    }

                    dgvItems.ItemsSource = items;
                }
                else
                {
                    if (!isSilent && this.IsVisible)
                    {
                        MessageBox.Show("Transaction not found.", "ERROR");
                        GoBackOrNavigateList();
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isSilent) MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                if (!isSilent) this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async Task PopulateRelatedDataAsync(TransactionModel t)
        {
            try
            {
                if (!string.IsNullOrEmpty(t.ClientID))
                {
                    var client = await _clientManager.GetClientByIDAsync(t.ClientID);
                    if (client != null)
                    {
                        t.ClientName = client.ClientName;
                        t.ClientContact = client.ClientContact;
                    }
                }

                if (!string.IsNullOrEmpty(t.AdminID))
                {
                    var emp = await _employeeManager.GetEmployeeByIDAsync(t.AdminID);
                    if (emp != null)
                    {
                        t.EmployeeName = emp.EmployeeName;
                    }
                }
            }
            catch { /* Ignore */ }
        }

        private void ApplyToUI(TransactionModel transaction)
        {
            if (lblTransactionID != null) lblTransactionID.Text = transaction.TransactionID ?? "N/A";

            if (lblTransactionDate != null) lblTransactionDate.Text = transaction.TransactionDate.ToLocalTime().ToString("dd MMM yyyy HH:mm");

            if (lblClientName != null) lblClientName.Text = transaction.ClientName ?? "-";
            if (lblClientContact != null) lblClientContact.Text = transaction.ClientContact ?? "-";
            if (lblEmployeeName != null) lblEmployeeName.Text = transaction.EmployeeName ?? "-";

            if (lblPaymentStatus != null) lblPaymentStatus.Text = transaction.PaymentStatus ?? "Unknown";
            if (lblDeliveryStatus != null) lblDeliveryStatus.Text = transaction.DeliveryStatus ?? "Pending";
            if (lblTotalAmount != null) lblTotalAmount.Text = $"Rp {transaction.TotalAmount:N0}";
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedTransaction == null) return;
            this.NavigationService.Navigate(new EditTransactionPage(_loadedTransaction, _currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedTransaction == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete transaction {_loadedTransaction.TransactionID}?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    btnDelete.IsEnabled = false;
                    btnEdit.IsEnabled = false;
                    this.Cursor = System.Windows.Input.Cursors.Wait;

                    bool success = await _dataManager.DeleteTransactionAsync(_loadedTransaction.TransactionID);

                    if (success)
                    {
                        MessageBox.Show("Transaction deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        TransactionPage.NotifyDataChanged();
                        GoBackOrNavigateList();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete transaction.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting transaction: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnDelete.IsEnabled = true;
                    btnEdit.IsEnabled = true;
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private void GoBackOrNavigateList()
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new TransactionPage(_currentUserProfile));
        }

        private void DisplayProfileData(Person profile)
        {
            if (lblUserName != null)
            {
                lblUserName.Text = (profile?.Names?.Count > 0)
                    ? profile.Names[0].DisplayName
                    : "Pengguna Tidak Dikenal";
            }

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
    }
}