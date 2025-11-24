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
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private Person currentUserProfile;
        private string SupplierID;

        public ViewSupplierPage(string id, Person userProfile)
        {
            InitializeComponent();
            SupplierID = id;
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadData();
        }

        private void LoadData()
        {
            Supplier s = supplierManager.GetSupplierByID(SupplierID);
            if (s != null)
            {
                lblSupplierID.Text = s.SupplierID;
                lblSupplierName.Text = s.SupplierName;
                lblSupplierPhone.Text = s.SupplierPhone;
                lblSupplierAddress.Text = s.SupplierAddress;
                lblSupplierType.Text = s.SupplierType;
            }
            else
            {
                MessageBox.Show($"ID Supplier {SupplierID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new EditSupplierPage(SupplierID, this.currentUserProfile));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this supplier?\nSupplier ID: {SupplierID}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = supplierManager.DeleteSupplier(SupplierID);
                if (result == 1)
                {
                    MessageBox.Show("Supplier deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayProfileData(Person profile)
        {
            if (profile.Names != null && profile.Names.Count > 0)
            {
                lblUserName.Text = profile.Names[0].DisplayName;
            }
            else
            {
                lblUserName.Text = "Pengguna Tidak Dikenal";
            }

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                string photoUrl = profile.Photos[0].Url;

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.EndInit();

                    imgUserProfile.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memuat foto profil: {ex.Message}", "Error Foto", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}