using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Stock
{
    public partial class EditStockPage : Page
    {
        private readonly Person currentUserProfile;
        private readonly ProductDataManager dataManager = new ProductDataManager();
        private readonly SupplierDataManager supplierDataManager = new SupplierDataManager();
        private Product? WorkingProduct;
        private bool isProcessing = false;

        public EditStockPage(Product product, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            WorkingProduct = product;

            DisplayProfileData(userProfile);

            this.Loaded += EditStockPage_Loaded;
        }

        private async void EditStockPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;
                txtProductName.IsEnabled = false;

                await LoadSuppliersAsync();
                LoadGrades();

                // Load fresh data from database
                if (WorkingProduct != null)
                {
                    var found = await dataManager.GetProductByIDAsync(WorkingProduct.ProductID);
                    if (found != null)
                    {
                        WorkingProduct = found;
                        PopulateFieldsFromModel();
                    }
                    else
                    {
                        MessageBox.Show($"Product with ID {WorkingProduct.ProductID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        GoBackOrNavigateList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                txtProductName.IsEnabled = true;
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliersTable = await supplierDataManager.LoadSupplierDataAsync();

                cmbSupplier.Items.Clear();
                cmbSupplier.Items.Add(new ComboBoxItem { Content = "-- Select Supplier --", Tag = null });

                foreach (DataRow row in suppliersTable.Rows)
                {
                    var item = new ComboBoxItem
                    {
                        Content = row["supplier_name"].ToString(),
                        Tag = row["supplierid"].ToString()
                    };
                    cmbSupplier.Items.Add(item);
                }

                cmbSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load suppliers: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGrades()
        {
            cmbGrade.Items.Clear();
            cmbGrade.Items.Add(new ComboBoxItem { Content = "A", Tag = "A" });
            cmbGrade.Items.Add(new ComboBoxItem { Content = "B", Tag = "B" });
            cmbGrade.Items.Add(new ComboBoxItem { Content = "C", Tag = "C" });
        }

        private void PopulateFieldsFromModel()
        {
            if (WorkingProduct == null) return;

            txtStockID.Text = WorkingProduct.ProductID;
            txtProductName.Text = WorkingProduct.ProductName;
            txtUnitPrice.Text = WorkingProduct.UnitPrice.ToString("0.##");
            txtQuantity.Text = WorkingProduct.Quantity.ToString("0.##");

            // Set grade
            foreach (ComboBoxItem item in cmbGrade.Items)
            {
                if (item.Tag?.ToString() == WorkingProduct.Grade)
                {
                    cmbGrade.SelectedItem = item;
                    break;
                }
            }

            // Set supplier
            if (!string.IsNullOrEmpty(WorkingProduct.SupplierID))
            {
                foreach (ComboBoxItem item in cmbSupplier.Items)
                {
                    if (item.Tag?.ToString() == WorkingProduct.SupplierID)
                    {
                        cmbSupplier.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingProduct == null || isProcessing) return;

            // Validation
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("Please enter product name.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProductName.Focus();
                return;
            }

            if (!decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice) || unitPrice <= 0)
            {
                MessageBox.Show("Please enter valid unit price.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUnitPrice.Focus();
                return;
            }

            if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity < 0)
            {
                MessageBox.Show("Please enter valid quantity.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtQuantity.Focus();
                return;
            }

            try
            {
                isProcessing = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                // Get selected supplier
                string? supplierID = null;
                if (cmbSupplier.SelectedItem is ComboBoxItem selectedSupplier && selectedSupplier.Tag != null)
                {
                    supplierID = selectedSupplier.Tag.ToString();
                }

                // Get selected grade
                string grade = ((ComboBoxItem)cmbGrade.SelectedItem).Tag.ToString();

                WorkingProduct.ProductName = txtProductName.Text.Trim();
                WorkingProduct.Grade = grade;
                WorkingProduct.Quantity = quantity;
                WorkingProduct.UnitPrice = unitPrice;
                WorkingProduct.SupplierID = supplierID;

                int result = await dataManager.UpdateProductAsync(WorkingProduct);
                bool success = result != 0;

                // Double check
                if (!success)
                {
                    var verify = await dataManager.GetProductByIDAsync(WorkingProduct.ProductID);
                    success = verify != null;
                    if (success && verify != null)
                    {
                        WorkingProduct = verify;
                    }
                }

                if (success)
                {
                    PopulateFieldsFromModel();
                    MessageBox.Show("Product updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    StockPage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update product. Connection might be lost.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isProcessing = false;
                btnSave.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingProduct == null || isProcessing) return;

            var confirm = MessageBox.Show(
                $"Delete product {WorkingProduct.ProductName}?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    isProcessing = true;
                    btnDelete.IsEnabled = false;
                    this.Cursor = System.Windows.Input.Cursors.Wait;

                    await dataManager.DeleteProductAsync(WorkingProduct.ProductID);

                    MessageBox.Show("Product deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    StockPage.NotifyDataChanged();
                    GoBackOrNavigateList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete Error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    isProcessing = false;
                    btnDelete.IsEnabled = true;
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
                NavigationService?.Navigate(new StockPage(currentUserProfile));
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
    }
}