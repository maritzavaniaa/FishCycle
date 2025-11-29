using FishCycleApp.DataAccess;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Use alias to avoid namespace conflict
using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    /// <summary>
    /// Interaction logic for EditTransactionPage.xaml
    /// </summary>
    public partial class EditTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly Person _currentUserProfile;
        private TransactionModel? _workingTransaction;
        private bool _isProcessing = false;

        // Constructor 1: Accept Transaction object directly
        public EditTransactionPage(TransactionModel transaction, Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            _workingTransaction = transaction;
            DisplayProfileData(userProfile);
            PopulateFieldsFromModel();
        }

        // Constructor 2: Accept transaction ID (needs to load data)
        public EditTransactionPage(string transactionID, Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            _ = LoadTransactionByIdAsync(transactionID);
        }

        private async Task LoadTransactionByIdAsync(string transactionID)
        {
            try
            {
                this.Cursor = Cursors.Wait;

                var found = await _dataManager.GetTransactionByIDAsync(transactionID?.Trim());

                if (found == null)
                {
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show($"Transaction with ID {transactionID} not found.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                    return;
                }

                _workingTransaction = found;
                PopulateFieldsFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void PopulateFieldsFromModel()
        {
            if (_workingTransaction == null) return;

            // Find and populate controls based on your XAML
            var txtTransactionID = this.FindName("txtTransactionID") as TextBox;
            var dpTransactionDate = this.FindName("dpTransactionDate") as DatePicker;
            var cmbPaymentStatus = this.FindName("cmbPaymentStatus") as ComboBox;

            if (txtTransactionID != null)
            {
                txtTransactionID.Text = _workingTransaction.TransactionID;
                txtTransactionID.IsReadOnly = true;
            }

            if (dpTransactionDate != null)
                dpTransactionDate.SelectedDate = _workingTransaction.TransactionDate;

            // TODO: Set payment status combobox
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_workingTransaction == null || _isProcessing) return;

            try
            {
                _isProcessing = true;
                btnSave.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                // Update transaction properties from UI controls
                // _workingTransaction.PaymentStatus = ... get from combobox

                int result = await _dataManager.UpdateTransactionAsync(_workingTransaction);

                bool success = result != 0;
                if (!success)
                {
                    var verify = await _dataManager.GetTransactionByIDAsync(_workingTransaction.TransactionID);
                    success = verify != null;
                    if (success && verify != null)
                    {
                        _workingTransaction = verify;
                    }
                }

                if (success)
                {
                    PopulateFieldsFromModel();
                    MessageBox.Show("Transaction updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    TransactionPage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update transaction.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                btnSave.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void DisplayProfileData(Person profile)
        {
            var lblUserName = this.FindName("lblUserName") as TextBlock;
            var imgUserProfile = this.FindName("imgUserProfile") as System.Windows.Controls.Image;

            if (lblUserName != null)
            {
                lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                    ? profile.Names[0].DisplayName
                    : "Pengguna Tidak Dikenal";
            }

            if (imgUserProfile != null && profile.Photos != null && profile.Photos.Count > 0)
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