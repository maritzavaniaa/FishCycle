using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class ViewSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();
        private readonly Person currentUserProfile;
        private readonly string SupplierID;
        private Supplier? currentSupplier;

        public ViewSupplierPage(string id, Person userProfile)
        {
            InitializeComponent();
            SupplierID = id?.Trim() ?? string.Empty;
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                currentSupplier = supplierManager.GetSupplierByID(SupplierID);
                if (currentSupplier != null)
                {
                    lblSupplierID.Text = currentSupplier.SupplierID;
                    lblSupplierName.Text = currentSupplier.SupplierName;
                    lblSupplierPhone.Text = currentSupplier.SupplierPhone ?? "-";
                    lblSupplierAddress.Text = currentSupplier.SupplierAddress ?? "-";
                    lblSupplierType.Text = currentSupplier.SupplierType;
                }
                else
                {
                    MessageBox.Show($"Supplier ID {SupplierID} not found.", "NOT FOUND", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading supplier: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentSupplier == null)
            {
                MessageBox.Show("Supplier data not loaded.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Navigasi ke halaman edit; jika halaman edit mampu menerima ID saja ini sudah cukup.
            NavigationService?.Navigate(new EditSupplierPage(SupplierID, currentUserProfile));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SupplierID))
            {
                MessageBox.Show("Invalid supplier ID.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this supplier?\nSupplier ID: {SupplierID}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes) return;

            int result = 0;
            try
            {
                result = supplierManager.DeleteSupplier(SupplierID);

                bool success = result != 0;
                if (!success)
                {
                    // Verifikasi konsisten dengan pola Employee: cek apakah sudah hilang
                    var stillThere = supplierManager.GetSupplierByID(SupplierID);
                    success = (stillThere == null);
                }

                if (success)
                {
                    MessageBox.Show("Supplier deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    SupplierPage.NotifyDataChanged(); // trigger reload list
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadData(); // refresh tampilan current
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                ? profile.Names[0].DisplayName
                : "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                string photoUrl = profile.Photos[0].Url;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load profile photo: {ex.Message}");
                }
            }
        }
    }
}