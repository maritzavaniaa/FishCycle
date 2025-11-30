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

namespace FishCycleApp
{
    public partial class ViewSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();
        private readonly Person currentUserProfile;

        private Supplier? LoadedSupplier;
        private string _currentSupplierID;

        private CancellationTokenSource _cts;

        public ViewSupplierPage(string supplierID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            _currentSupplierID = supplierID?.Trim() ?? "";

            DisplayProfileData(userProfile);

            Loaded += ViewSupplierPage_Loaded;
            Unloaded += ViewSupplierPage_Unloaded;
            IsVisibleChanged += ViewSupplierPage_IsVisibleChanged;
        }

        private void ViewSupplierPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDataSafe(false);
        }

        private void ViewSupplierPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void ViewSupplierPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
                ReloadDataSafe(true);
        }

        private void ReloadDataSafe(bool isSilent)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _ = LoadSupplierDetailsAsync(_currentSupplierID, isSilent, _cts.Token);
        }

        private async Task LoadSupplierDetailsAsync(string supplierID, bool isSilent, CancellationToken token)
        {
            try
            {
                if (!isSilent)
                    Cursor = System.Windows.Input.Cursors.Wait;

                var result = await supplierManager.GetSupplierByIDAsync(supplierID);

                if (token.IsCancellationRequested) return;

                LoadedSupplier = result;

                if (LoadedSupplier != null)
                {
                    _currentSupplierID = LoadedSupplier.SupplierID;

                    ApplyToUI(LoadedSupplier);
                }
                else if (!isSilent)
                {
                    MessageBox.Show($"Supplier with ID {supplierID} not found.", "NOT FOUND");
                    GoBackOrNavigateList();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!isSilent)
                    MessageBox.Show($"Error loading supplier: {ex.Message}", "ERROR");
            }
            finally
            {
                if (!isSilent)
                    Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void ApplyToUI(Supplier s)
        {
            lblSupplierID.Text = s.SupplierID;
            lblSupplierName.Text = s.SupplierName;
            lblSupplierPhone.Text = s.SupplierPhone ?? "-";
            lblSupplierAddress.Text = s.SupplierAddress ?? "-";
            lblSupplierType.Text = s.SupplierType;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedSupplier == null) return;
            this.NavigationService.Navigate(new EditSupplierPage(LoadedSupplier, currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedSupplier == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete supplier '{LoadedSupplier.SupplierName}'?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                btnDelete.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                await supplierManager.DeleteSupplierAsync(LoadedSupplier.SupplierID);

                var stillThere = await supplierManager.GetSupplierByIDAsync(LoadedSupplier.SupplierID);
                bool success = stillThere == null;

                if (success)
                {
                    MessageBox.Show("Supplier deleted successfully!", "SUCCESS");
                    SupplierPage.NotifyDataChanged();
                    GoBackOrNavigateList();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "ERROR");
            }
            finally
            {
                btnDelete.IsEnabled = true;
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void GoBackOrNavigateList()
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new SupplierPage(currentUserProfile));
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "Unknown User";

            if (profile?.Photos?.Count > 0)
            {
                try
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(profile.Photos[0].Url, UriKind.Absolute);
                    bmp.EndInit();
                    imgUserProfile.Source = bmp;
                }
                catch { }
            }
        }
    }
}