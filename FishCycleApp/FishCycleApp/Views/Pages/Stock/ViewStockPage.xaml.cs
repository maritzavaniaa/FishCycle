using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishCycleApp.Views.Pages.Stock
{
    public partial class ViewStockPage : Page
    {
        private readonly ProductDataManager dataManager = new ProductDataManager();
        private readonly Person currentUserProfile;
        private Product? LoadedProduct;
        private string _currentProductID;
        private CancellationTokenSource? _cts;

        public ViewStockPage(string productID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            _currentProductID = productID?.Trim();

            DisplayProfileData(userProfile);

            this.Loaded += ViewStockPage_Loaded;
            this.Unloaded += ViewStockPage_Unloaded;
            this.IsVisibleChanged += ViewStockPage_IsVisibleChanged;
        }

        private void ViewStockPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDataSafe(isSilent: false);
        }

        private void ViewStockPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void ViewStockPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ReloadDataSafe(isSilent: true);
            }
        }

        private void ReloadDataSafe(bool isSilent)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = LoadProductDetailsAsync(_currentProductID, isSilent, _cts.Token);
        }

        private async Task LoadProductDetailsAsync(string productID, bool isSilent, CancellationToken token)
        {
            try
            {
                if (!isSilent) this.Cursor = System.Windows.Input.Cursors.Wait;

                var result = await dataManager.GetProductByIDAsync(productID, token);

                if (token.IsCancellationRequested) return;

                LoadedProduct = result;

                if (LoadedProduct != null)
                {
                    if (!string.IsNullOrEmpty(LoadedProduct.SupplierID))
                    {
                        var suppManager = new SupplierDataManager();
                        var supplier = await suppManager.GetSupplierByIDAsync(LoadedProduct.SupplierID);

                        if (supplier != null)
                        {
                            LoadedProduct.SupplierName = supplier.SupplierName;
                        }
                    }

                    ApplyToUI(LoadedProduct);
                }
                else
                {
                    if (!isSilent && this.IsVisible)
                    {
                        this.Cursor = System.Windows.Input.Cursors.Arrow;
                        MessageBox.Show($"Product with ID {productID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        GoBackOrNavigateList();
                    }
                }
            }
            catch (OperationCanceledException)
            {
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

        private void ApplyToUI(Product product)
        {
            lblStockID.Text = product.ProductID;
            lblProductName.Text = product.ProductName;
            lblSupplier.Text = product.SupplierName ?? "N/A";
            lblUnitPrice.Text = product.UnitPrice.ToString("N0");
            lblQuantity.Text = product.Quantity.ToString("N2");
            lblGrade.Text = product.Grade;

            switch (product.Grade?.ToUpper())
            {
                case "A":
                    GradeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E86C"));
                    lblGrade.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D5016"));
                    break;
                case "B":
                    GradeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8D5A1"));
                    lblGrade.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79552E"));
                    break;
                case "C":
                    GradeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                    lblGrade.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                    break;
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedProduct == null) return;
            this.NavigationService.Navigate(new EditStockPage(LoadedProduct, currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedProduct == null) return;

            var confirm = MessageBox.Show(
                $"Delete product {LoadedProduct.ProductName}?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    btnDelete.IsEnabled = false;
                    this.Cursor = System.Windows.Input.Cursors.Wait;

                    await dataManager.DeleteProductAsync(LoadedProduct.ProductID);

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
                    btnDelete.IsEnabled = true;
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                }
            }
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