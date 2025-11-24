using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class EditSupplierPage : Page
    {
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private Person currentUserProfile;
        private string SupplierID;

        public EditSupplierPage(string id, Person userProfile)
        {
            InitializeComponent();
            SupplierID = id;
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadCategory();
            LoadSupplierData();
        }

        private void LoadCategory()
        {
            cmbSupplierType.Items.Clear();
            cmbSupplierType.Items.Add("Fresh Catch");
            cmbSupplierType.Items.Add("First-Hand");
            cmbSupplierType.Items.Add("Reprocessed Stock");
        }

        private void LoadSupplierData()
        {
            Supplier s = supplierManager.GetSupplierByID(SupplierID);
            if (s != null)
            {
                txtSupplierID.Text = s.SupplierID;
                txtSupplierName.Text = s.SupplierName;
                txtSupplierPhone.Text = s.SupplierPhone;
                txtSupplierAddress.Text = s.SupplierAddress;
                cmbSupplierType.SelectedItem = s.SupplierType;
            }
            else
            {
                MessageBox.Show($"Supplier with ID {SupplierID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input kosong
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Please enter supplier name.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSupplierPhone.Text))
            {
                MessageBox.Show("Please enter supplier phone.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierPhone.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSupplierAddress.Text))
            {
                MessageBox.Show("Please enter supplier address.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierAddress.Focus();
                return;
            }

            if (cmbSupplierType.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Supplier s = new Supplier
            {
                SupplierID = SupplierID,
                SupplierType = cmbSupplierType.SelectedItem?.ToString() ?? "",
                SupplierName = txtSupplierName.Text.Trim(),
                SupplierPhone = txtSupplierPhone.Text.Trim(),
                SupplierAddress = txtSupplierAddress.Text.Trim(),
            };

            int result = supplierManager.UpdateSupplier(s);

            if (result == 1)
            {
                MessageBox.Show("Supplier updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                this.NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this supplier?\nSupplier ID: {SupplierID}\nSupplier Name: {txtSupplierName.Text}",
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

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
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