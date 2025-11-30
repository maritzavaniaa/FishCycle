using FishCycleApp.DataAccess;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TransactionModel = FishCycleApp.Models.Transaction;

namespace FishCycleApp.Views.Pages.Transaction
{
    public partial class AddTransactionPage : Page
    {
        private readonly TransactionDataManager _dataManager = new TransactionDataManager();
        private readonly ClientDataManager _clientDataManager = new ClientDataManager();
        private readonly EmployeeDataManager _employeeDataManager = new EmployeeDataManager();
        private readonly Person _currentUserProfile;
        private bool _isSaving = false;

        public AddTransactionPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += AddTransactionPage_Loaded;
        }

        private async void AddTransactionPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Generate Transaction ID
            txtTransactionID.Text = GenerateTransactionID();

            // Set default date to today
            dpTransactionDate.SelectedDate = DateTime.Now;

            // Load dropdown data
            await LoadClientsAsync();
            await LoadEmployeesAsync();
        }

        private string GenerateTransactionID()
        {
            return $"TRX-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        // ==========================================
        // LOAD REAL DATA FROM DATABASE
        // ==========================================
        private async Task LoadClientsAsync()
        {
            try
            {
                // DataManager sekarang mengembalikan List<Client>, BUKAN DataTable
                var clients = await _clientDataManager.LoadClientDataAsync();

                cmbClient.Items.Clear();

                // Loop langsung ke dalam List (tanpa .Rows)
                foreach (var client in clients)
                {
                    cmbClient.Items.Add(new
                    {
                        // Akses property langsung pakai Huruf Besar (PascalCase) sesuai Model
                        ClientID = client.ClientID,
                        ClientName = client.ClientName
                    });
                }

                if (cmbClient.Items.Count > 0)
                    cmbClient.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading clients: {ex.Message}");
                MessageBox.Show("Failed to load clients. Please try again.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                // DataManager sekarang mengembalikan List<Employee>
                var employees = await _employeeDataManager.LoadEmployeeDataAsync();

                cmbEmployee.Items.Clear();

                // Loop langsung ke dalam List
                foreach (var emp in employees)
                {
                    cmbEmployee.Items.Add(new
                    {
                        // Akses property langsung
                        EmployeeID = emp.EmployeeID,
                        EmployeeName = emp.EmployeeName
                    });
                }

                if (cmbEmployee.Items.Count > 0)
                    cmbEmployee.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading employees: {ex.Message}");
                MessageBox.Show("Failed to load employees. Please try again.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ==========================================
        // SAVE WITH FULL VALIDATION
        // ==========================================
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;

            // Transaction ID
            if (string.IsNullOrWhiteSpace(txtTransactionID.Text))
            {
                txtTransactionID.Text = GenerateTransactionID();
            }

            // Client validation
            if (cmbClient.SelectedValue == null)
            {
                MessageBox.Show("Please select a client.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbClient.Focus();
                return;
            }

            string clientID = cmbClient.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(clientID))
            {
                MessageBox.Show("Invalid client selection.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Employee validation
            if (cmbEmployee.SelectedValue == null)
            {
                MessageBox.Show("Please select an employee.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbEmployee.Focus();
                return;
            }

            string employeeID = cmbEmployee.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(employeeID))
            {
                MessageBox.Show("Invalid employee selection.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Date validation
            if (dpTransactionDate.SelectedDate == null)
            {
                MessageBox.Show("Please select a transaction date.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpTransactionDate.Focus();
                return;
            }

            if (dpTransactionDate.SelectedDate.Value.Date > DateTime.Now.Date)
            {
                MessageBox.Show("Transaction date cannot be in the future.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpTransactionDate.Focus();
                return;
            }

            // Amount validation
            if (string.IsNullOrWhiteSpace(txtTotalAmount.Text))
            {
                MessageBox.Show("Please enter total amount.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTotalAmount.Focus();
                return;
            }

            if (!decimal.TryParse(txtTotalAmount.Text, out decimal totalAmount))
            {
                MessageBox.Show("Please enter a valid amount (numbers only).", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTotalAmount.Focus();
                return;
            }

            if (totalAmount <= 0)
            {
                MessageBox.Show("Total amount must be greater than zero.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTotalAmount.Focus();
                return;
            }

            // Payment status validation
            if (cmbPaymentStatus.SelectedItem == null)
            {
                MessageBox.Show("Please select payment status.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbPaymentStatus.Focus();
                return;
            }

            string paymentStatus = ((ComboBoxItem)cmbPaymentStatus.SelectedItem).Content.ToString();

            try
            {
                _isSaving = true;
                btnSave.IsEnabled = false;
                btnCancel.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                // Check duplicate
                var existingTransaction = await _dataManager.GetTransactionByIDAsync(txtTransactionID.Text.Trim());
                if (existingTransaction != null)
                {
                    MessageBox.Show(
                        $"Transaction ID {txtTransactionID.Text} already exists.\nGenerating new ID...",
                        "WARNING",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    txtTransactionID.Text = GenerateTransactionID();
                    return;
                }

                // Create new transaction
                var newTransaction = new TransactionModel
                {
                    TransactionID = txtTransactionID.Text.Trim(),
                    AdminID = employeeID,
                    ClientID = clientID,
                    TotalAmount = totalAmount,
                    TransactionDate = dpTransactionDate.SelectedDate.Value,
                    PaymentStatus = paymentStatus,
                    DeliveryStatus = "Pending"
                };

                int result = await _dataManager.InsertTransactionAsync(newTransaction);

                bool success = result != 0;
                if (!success)
                {
                    var verify = await _dataManager.GetTransactionByIDAsync(newTransaction.TransactionID);
                    success = verify != null;
                }

                if (success)
                {
                    MessageBox.Show("Transaction added successfully!", "SUCCESS",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    TransactionPage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to add transaction.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding transaction: {ex.Message}", "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSaving = false;
                btnSave.IsEnabled = true;
                btnCancel.IsEnabled = true;
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