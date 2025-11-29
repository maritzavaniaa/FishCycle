using FishCycleApp.DataAccess;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

// Use alias to avoid namespace conflict
using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    /// <summary>
    /// Interaction logic for ViewTransactionPage.xaml
    /// </summary>
    public partial class ViewTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly Person _currentUserProfile;
        private TransactionModel? _loadedTransaction;
        private string _currentTransactionID;

        private CancellationTokenSource? _cts;

        // Constructor with transaction ID and user profile
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
                    ApplyToUI(_loadedTransaction);
                }
                else
                {
                    if (!isSilent && this.IsVisible)
                    {
                        this.Cursor = System.Windows.Input.Cursors.Arrow;
                        MessageBox.Show($"Transaction with ID {transactionID} not found.", "ERROR",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        GoBackOrNavigateList();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal when cancelled, ignore
            }
            catch (Exception ex)
            {
                if (!isSilent && this.IsVisible)
                    MessageBox.Show($"Error loading details: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!isSilent && this.IsVisible)
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void ApplyToUI(TransactionModel transaction)
        {
            // Find controls by name and set values
            var lblTransactionID = this.FindName("lblTransactionID") as TextBlock;
            var lblTransactionDate = this.FindName("lblTransactionDate") as TextBlock;
            var lblClientName = this.FindName("lblClientName") as TextBlock;
            var lblEmployeeName = this.FindName("lblEmployeeName") as TextBlock;
            var lblPaymentStatus = this.FindName("lblPaymentStatus") as TextBlock;
            var lblDeliveryStatus = this.FindName("lblDeliveryStatus") as TextBlock;
            var lblTotalAmount = this.FindName("lblTotalAmount") as TextBlock;

            // Safely update UI with null checks
            if (lblTransactionID != null)
                lblTransactionID.Text = transaction.TransactionID ?? "N/A";

            if (lblTransactionDate != null)
                lblTransactionDate.Text = transaction.TransactionDate.ToString("dd MMM yyyy HH:mm");

            if (lblClientName != null)
                lblClientName.Text = transaction.ClientName ?? "Unknown Client";

            if (lblEmployeeName != null)
                lblEmployeeName.Text = transaction.EmployeeName ?? "Unknown Employee";

            if (lblPaymentStatus != null)
                lblPaymentStatus.Text = transaction.PaymentStatus ?? "Unknown";

            if (lblDeliveryStatus != null)
                lblDeliveryStatus.Text = transaction.DeliveryStatus ?? "Pending";

            if (lblTotalAmount != null)
                lblTotalAmount.Text = $"Rp {transaction.TotalAmount:N0}";
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
                $"Are you sure you want to delete transaction {_loadedTransaction.TransactionID}?\n\n" +
                $"Client: {_loadedTransaction.ClientName ?? "Unknown"}\n" +
                $"Amount: Rp {_loadedTransaction.TotalAmount:N0}",
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

                    int result = await _dataManager.DeleteTransactionAsync(_loadedTransaction.TransactionID);

                    if (result != 0)
                    {
                        MessageBox.Show("Transaction deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        TransactionPage.NotifyDataChanged();
                        GoBackOrNavigateList();
                    }
                    else
                    {
                        // Verify if actually deleted
                        var verify = await _dataManager.GetTransactionByIDAsync(_loadedTransaction.TransactionID);
                        if (verify == null)
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
            var lblUserName = this.FindName("lblUserName") as TextBlock;
            var imgUserProfile = this.FindName("imgUserProfile") as System.Windows.Controls.Image;

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
                catch { /* Ignore */ }
            }
        }
    }
}