using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using FishCycleApp.Views.Pages.Stock;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class AddTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly ClientDataManager _clientDataManager = new ClientDataManager();
        private readonly EmployeeDataManager _employeeDataManager = new EmployeeDataManager();
        private readonly ProductDataManager _productDataManager = new ProductDataManager();
        private System.Collections.ObjectModel.ObservableCollection<TransactionDetail> _cartItems = new System.Collections.ObjectModel.ObservableCollection<TransactionDetail>();

        private readonly Person _currentUserProfile;
        private bool _isSaving = false;
        private int _currentStep = 1; 

        private readonly SolidColorBrush _activeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0077B6"));
        private readonly SolidColorBrush _activeBgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CAF0F8"));
        private readonly SolidColorBrush _inactiveColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ADB5BD"));
        private readonly SolidColorBrush _inactiveBgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
        private readonly SolidColorBrush _inactiveBorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));

        public AddTransactionPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            InitializeComboBoxes();

            this.Loaded += AddTransactionPage_Loaded;
        }

        private void InitializeComboBoxes()
        {
            cmbPaymentStatus.Items.Clear();
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Pending", IsSelected = true });
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Paid" });
            cmbPaymentStatus.Items.Add(new ComboBoxItem { Content = "Cancelled" });

            cmbDistributionMethod.Items.Clear();
            cmbDistributionMethod.Items.Add(new ComboBoxItem { Content = "Self Pickup", IsSelected = true });
            cmbDistributionMethod.Items.Add(new ComboBoxItem { Content = "Distribution Agent" });
        }

        private async void AddTransactionPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtTransactionID.Text = GenerateTransactionID();

            dpTransactionDate.SelectedDate = DateTime.Now;

            dgvTransactionItems.ItemsSource = _cartItems;

            await LoadClientsAsync();
            await LoadEmployeesAsync();
            await LoadProductsAsync();

            NavigateToStep(1);
        }

        private string GenerateTransactionID()
        {
            return $"TRX-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        private void NavigateToStep(int stepNumber)
        {
            _currentStep = stepNumber;

            Section1_Transaction.Visibility = (stepNumber == 1) ? Visibility.Visible : Visibility.Collapsed;
            Section2_Items.Visibility = (stepNumber == 2) ? Visibility.Visible : Visibility.Collapsed;
            Section3_Distribution.Visibility = (stepNumber == 3) ? Visibility.Visible : Visibility.Collapsed;

            UpdateStepperUI(stepNumber);
        }

        private void UpdateStepperUI(int activeStep)
        {
            ResetStepperStyle(Step1Circle, Step1Dot, Step1Text);
            ResetStepperStyle(Step2Circle, Step2Dot, Step2Text);
            ResetStepperStyle(Step3Circle, Step3Dot, Step3Text);

            switch (activeStep)
            {
                case 1:
                    SetStepperActive(Step1Circle, Step1Dot, Step1Text);
                    break;
                case 2:
                    SetStepperActive(Step1Circle, Step1Dot, Step1Text);
                    SetStepperActive(Step2Circle, Step2Dot, Step2Text);
                    break;
                case 3:
                    SetStepperActive(Step1Circle, Step1Dot, Step1Text);
                    SetStepperActive(Step2Circle, Step2Dot, Step2Text);
                    SetStepperActive(Step3Circle, Step3Dot, Step3Text);
                    break;
            }
        }

        private void ResetStepperStyle(Border circle, Ellipse dot, TextBlock text)
        {
            circle.Background = Brushes.White;
            circle.BorderBrush = _inactiveBorderColor;
            dot.Fill = _inactiveBgColor;
            text.Foreground = _inactiveColor;
            text.FontWeight = FontWeights.SemiBold;
        }

        private void SetStepperActive(Border circle, Ellipse dot, TextBlock text)
        {
            circle.Background = _activeBgColor;
            circle.BorderBrush = _activeColor;
            dot.Fill = _activeColor;
            text.Foreground = _activeColor;
            text.FontWeight = FontWeights.Bold;
        }

        private void btnNext1_Click(object sender, RoutedEventArgs e)
        {
            if (cmbEmployee.SelectedItem == null) { MessageBox.Show("Please select an employee.", "WARNING"); return; }
            if (dpTransactionDate.SelectedDate == null) { MessageBox.Show("Please select a date.", "WARNING"); return; }
            if (cmbClient.SelectedItem == null) { MessageBox.Show("Please select a client.", "WARNING"); return; }

            NavigateToStep(2);
        }

        private void btnPrev2_Click(object sender, RoutedEventArgs e)
        {
            NavigateToStep(1);
        }

        private void btnNext2_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Please add at least one item to the transaction.", "WARNING");
                return;
            }

            if (!decimal.TryParse(txtTotalAmount.Text, out decimal total) || total <= 0)
            {
                MessageBox.Show("Total amount is invalid.", "WARNING");
                return;
            }

            NavigateToStep(3);
        }

        private void btnPrev3_Click(object sender, RoutedEventArgs e)
        {
            NavigateToStep(2);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;

            if (cmbClient.SelectedItem == null || cmbEmployee.SelectedItem == null || dpTransactionDate.SelectedDate == null)
            {
                MessageBox.Show("Please complete Transaction Details (Step 1).", "WARNING");
                NavigateToStep(1); return;
            }

            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty! Please add items first.", "WARNING");
                NavigateToStep(2); return;
            }

            foreach (var item in _cartItems)
            {
                var currentProduct = await _productDataManager.GetProductByIDAsync(item.ProductID);

                if (currentProduct != null)
                {
                    if (item.Quantity > currentProduct.Quantity)
                    {
                        MessageBox.Show($"Stock for '{item.ProductName}' is not enough!\n" +
                                        $"Available: {currentProduct.Quantity}\n" +
                                        $"Requested: {item.Quantity}\n\n" +
                                        "Please decrease the quantity.", "STOCK ERROR");
                        NavigateToStep(2);
                        return;
                    }
                }
            }

            try
            {
                _isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                string clientID = ((ComboBoxItem)cmbClient.SelectedItem).Tag.ToString() ?? "";
                string employeeID = ((ComboBoxItem)cmbEmployee.SelectedItem).Tag.ToString() ?? "";
                string paymentStatus = (cmbPaymentStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Pending";
                string distributionMethod = (cmbDistributionMethod.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Self Pickup";

                decimal totalAmount = _cartItems.Sum(x => x.Subtotal);

                var newTransaction = new TransactionModel
                {
                    TransactionID = txtTransactionID.Text.Trim(),
                    AdminID = employeeID,
                    ClientID = clientID,
                    TotalAmount = totalAmount,
                    TransactionDate = dpTransactionDate.SelectedDate.Value.ToUniversalTime(),
                    PaymentStatus = paymentStatus,
                    DeliveryStatus = "Pending"
                };

                bool headerSuccess = await _dataManager.InsertTransactionAsync(newTransaction);

                if (headerSuccess)
                {
                    var dbItems = new System.Collections.Generic.List<TransactionItem>();

                    foreach (var cartItem in _cartItems)
                    {
                        dbItems.Add(new TransactionItem
                        {
                            TransactionID = newTransaction.TransactionID,
                            ProductID = cartItem.ProductID,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.UnitPrice
                        });
                    }

                    await _dataManager.InsertTransactionItemsAsync(dbItems);

                    foreach (var item in dbItems)
                    {
                        await _productDataManager.UpdateStockQuantityAsync(item.ProductID, item.Quantity);
                    }

                    MessageBox.Show("Transaction Saved & Stock Updated!", "SUCCESS");

                    TransactionPage.NotifyDataChanged();
                    StockPage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                    else
                        NavigationService?.Navigate(new TransactionPage(_currentUserProfile));
                }
                else
                {
                    MessageBox.Show("Failed to save transaction header.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "EXCEPTION");
            }
            finally
            {
                _isSaving = false;
                btnSave.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
            }
        }

        private async Task LoadClientsAsync()
        {
            try
            {
                var clients = await _clientDataManager.LoadClientDataAsync();
                cmbClient.Items.Clear();
                foreach (var client in clients)
                {
                    cmbClient.Items.Add(new ComboBoxItem { Content = client.ClientName, Tag = client.ClientID });
                }
            }
            catch (Exception ex) { MessageBox.Show($"Failed to load clients: {ex.Message}"); }
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _employeeDataManager.LoadEmployeeDataAsync();
                cmbEmployee.Items.Clear();
                foreach (var emp in employees)
                {
                    cmbEmployee.Items.Add(new ComboBoxItem { Content = emp.EmployeeName, Tag = emp.EmployeeID });
                }
                if (cmbEmployee.Items.Count > 0) cmbEmployee.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show($"Failed to load employees: {ex.Message}"); }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _productDataManager.LoadProductDataAsync();
                cmbProduct.Items.Clear();

                foreach (var p in products)
                {
                    cmbProduct.Items.Add(new ComboBoxItem
                    {
                        Content = p.ProductName,
                        Tag = p 
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show($"Failed to load products: {ex.Message}"); }
        }

        private void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProduct.SelectedItem is ComboBoxItem item && item.Tag is Product p)
            {
                txtItemPrice.Text = p.UnitPrice.ToString("N0");
            }
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProduct.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not Product product)
            {
                MessageBox.Show("Please select a product.");
                return;
            }

            if (!decimal.TryParse(txtItemQty.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity.");
                return;
            }

            if (qty > product.Quantity)
            {
                MessageBox.Show($"Insufficient stock!\nAvailable: {product.Quantity} kg\nRequested: {qty} kg",
                    "STOCK WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingItem = _cartItems.FirstOrDefault(x => x.ProductID == product.ProductID);
            decimal currentQtyInCart = existingItem != null ? existingItem.Quantity : 0;

            if ((currentQtyInCart + qty) > product.Quantity)
            {
                MessageBox.Show($"Total quantity exceeds stock!\nStock: {product.Quantity}\nAlready in cart: {currentQtyInCart}",
                   "STOCK WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (existingItem != null)
            {
                _cartItems.Remove(existingItem);
                existingItem.Quantity += qty; 
                _cartItems.Add(existingItem);
            }
            else
            {
                var detail = new TransactionDetail
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    UnitPrice = product.UnitPrice,
                    Quantity = qty
                };
                _cartItems.Add(detail);
            }

            CalculateTotal();

            txtItemQty.Text = "";
            cmbProduct.SelectedIndex = -1;
            txtItemPrice.Text = "0";
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TransactionDetail item)
            {
                _cartItems.Remove(item);
                CalculateTotal();
            }
        }

        private void CalculateTotal()
        {
            decimal total = 0;
            foreach (var item in _cartItems)
            {
                total += item.Subtotal;
            }

            txtTotalAmountDisplay.Text = $"Rp {total:N0}";

            txtTotalAmount.Text = total.ToString();
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "Unknown User";
            if (profile?.Photos?.Count > 0)
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