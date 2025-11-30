using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using FishCycleApp.Views.Pages.Stock;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class EditTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly ProductDataManager _productManager = new ProductDataManager();
        private readonly Person _currentUserProfile;
        private TransactionModel? _workingTransaction;
        private bool _isProcessing = false;

        public EditTransactionPage(TransactionModel transaction, Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            _workingTransaction = transaction;
            DisplayProfileData(userProfile);
            InitializeComboBoxes();
            PopulateFieldsFromModel();
        }

        public EditTransactionPage(string transactionID, Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            InitializeComboBoxes(); 
            _ = LoadTransactionByIdAsync(transactionID);
        }

        private void InitializeComboBoxes()
        {
            cmbPaymentStatus.Items.Clear();
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Pending" });
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Paid" });
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Cancelled" });

            cmbDeliveryStatus.Items.Clear();
            cmbDeliveryStatus.Items.Add(new ComboBoxItem { Content = "Pending" });
            cmbDeliveryStatus.Items.Add(new ComboBoxItem { Content = "In Transit" });
            cmbDeliveryStatus.Items.Add(new ComboBoxItem { Content = "Delivered" });
            cmbDeliveryStatus.Items.Add(new ComboBoxItem { Content = "Cancelled" });
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
                    MessageBox.Show($"Transaction with ID {transactionID} not found.", "ERROR");
                    if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                    return;
                }

                _workingTransaction = found;
                PopulateFieldsFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "ERROR");
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void PopulateFieldsFromModel()
        {
            if (_workingTransaction == null) return;

            txtTransactionID.Text = _workingTransaction.TransactionID;
            txtTotal.Text = $"Rp {_workingTransaction.TotalAmount:N0}";
            txtClient.Text = _workingTransaction.ClientName ?? "-";

            dpTransactionDate.SelectedDate = _workingTransaction.TransactionDate.ToLocalTime();

            foreach (ComboBoxItem item in cmbPaymentStatus.Items)
            {
                if (item.Content.ToString() == _workingTransaction.PaymentStatus)
                {
                    cmbPaymentStatus.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in cmbDeliveryStatus.Items)
            {
                if (item.Content.ToString() == _workingTransaction.DeliveryStatus)
                {
                    cmbDeliveryStatus.SelectedItem = item;
                    break;
                }
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_workingTransaction == null || _isProcessing) return;

            try
            {
                _isProcessing = true;
                btnSave.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                string oldStatus = _workingTransaction.PaymentStatus;

                string newStatus = "Pending";
                if (cmbPaymentStatus.SelectedItem is ComboBoxItem item)
                    newStatus = item.Content.ToString() ?? "Pending";

                if (oldStatus != newStatus)
                {
                    if (newStatus == "Cancelled")
                    {
                        var items = await _dataManager.GetTransactionItemsAsync(_workingTransaction.TransactionID);
                        foreach (var i in items)
                        {
                            await _productManager.IncreaseStockAsync(i.ProductID, i.Quantity);
                        }
                        MessageBox.Show("Transaction Cancelled. Stock has been restored.", "INFO");
                    }
                    else if (oldStatus == "Cancelled")
                    {
                        var items = await _dataManager.GetTransactionItemsAsync(_workingTransaction.TransactionID);

                        foreach (var i in items)
                        {
                            var prod = await _productManager.GetProductByIDAsync(i.ProductID);
                            if (prod != null && prod.Quantity < i.Quantity)
                            {
                                MessageBox.Show($"Cannot reactivate! Not enough stock for {prod.ProductName}.", "ERROR");
                                return; 
                            }
                        }

                        foreach (var i in items)
                        {
                            await _productManager.DecreaseStockAsync(i.ProductID, i.Quantity);
                        }
                        MessageBox.Show("Transaction Reactivated. Stock has been deducted.", "INFO");
                    }
                }

                _workingTransaction.PaymentStatus = newStatus;
                if (dpTransactionDate.SelectedDate.HasValue)
                    _workingTransaction.TransactionDate = dpTransactionDate.SelectedDate.Value.ToUniversalTime();

                if (cmbDeliveryStatus.SelectedItem is ComboBoxItem delItem)
                    _workingTransaction.DeliveryStatus = delItem.Content.ToString();


                bool success = await _dataManager.UpdateTransactionAsync(_workingTransaction);

                if (success)
                {
                    MessageBox.Show("Transaction updated successfully!", "SUCCESS");
                    TransactionPage.NotifyDataChanged();
                    StockPage.NotifyDataChanged();
                    if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update transaction.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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