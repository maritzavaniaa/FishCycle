using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Stock
{
    public partial class AddStockPage : Page
    {
        private readonly Person currentUserProfile;
        private readonly ProductDataManager dataManager = new ProductDataManager();
        private readonly SupplierDataManager supplierDataManager = new SupplierDataManager();
        private bool isSaving = false;

        public AddStockPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += AddStockPage_Loaded;
        }

        private async void AddStockPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
            LoadGrades();
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = await supplierDataManager.LoadSupplierDataAsync();

                cmbSupplier.Items.Clear();

                cmbSupplier.Items.Add(new ComboBoxItem { Content = "-- Select Supplier --", Tag = null });

                foreach (var s in suppliers)
                {
                    var item = new ComboBoxItem
                    {
                        Content = s.SupplierName,
                        Tag = s.SupplierID
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
            cmbGrade.SelectedIndex = 0;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

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

            if (cmbGrade.SelectedItem == null)
            {
                MessageBox.Show("Please select grade.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                string productID = await GenerateProductIDAsync();

                string? supplierID = null;
                if (cmbSupplier.SelectedItem is ComboBoxItem selectedSupplier && selectedSupplier.Tag != null)
                {
                    supplierID = selectedSupplier.Tag.ToString();
                }

                string grade = ((ComboBoxItem)cmbGrade.SelectedItem).Tag.ToString();

                var newProduct = new Product
                {
                    ProductID = productID,
                    ProductName = txtProductName.Text.Trim(),
                    Grade = grade,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    SupplierID = supplierID
                };

                bool success = await dataManager.InsertProductAsync(newProduct);

                if (!success)
                {
                    var fetched = await dataManager.GetProductByIDAsync(newProduct.ProductID);
                    success = fetched != null;
                }

                if (success)
                {
                    MessageBox.Show($"Product added successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    StockPage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        NavigationService?.Navigate(new StockPage(currentUserProfile));
                    }
                }
                else
                {
                    MessageBox.Show("Failed to add product.\nCheck database connection.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred:\n{ex.Message}", "EXCEPTION", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isSaving = false;
                btnSave.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async Task<string> GenerateProductIDAsync()
        {
            try
            {
                var allProducts = await dataManager.LoadProductDataAsync();
                int maxNumber = 0;

                foreach (var prod in allProducts)
                {
                    string productID = prod.ProductID ?? "";

                    if (productID.StartsWith("PID-") && productID.Length > 4)
                    {
                        if (int.TryParse(productID.Substring(4), out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }
                }

                return $"PID-{(maxNumber + 1):D5}";
            }
            catch
            {
                return $"PID-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
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