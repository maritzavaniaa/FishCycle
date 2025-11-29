using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class EditSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();
        private readonly Person currentUserProfile;
        private readonly string SupplierID;

        public EditSupplierPage(string id, Person userProfile)
        {
            InitializeComponent();
            SupplierID = (id ?? string.Empty).Trim();
            currentUserProfile = userProfile;
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
            try
            {
                var s = supplierManager.GetSupplierByID(SupplierID);
                if (s != null)
                {
                    txtSupplierID.Text = s.SupplierID;
                    txtSupplierName.Text = s.SupplierName;
                    txtSupplierPhone.Text = s.SupplierPhone ?? string.Empty;
                    txtSupplierAddress.Text = s.SupplierAddress ?? string.Empty;

                    // Cocokkan item tipe berdasarkan teks
                    var match = cmbSupplierType.Items.Cast<object>()
                                   .FirstOrDefault(it => string.Equals(it?.ToString(), s.SupplierType, StringComparison.Ordinal));
                    cmbSupplierType.SelectedItem = match ?? s.SupplierType;
                }
                else
                {
                    MessageBox.Show($"Supplier with ID {SupplierID} not found.", "NOT FOUND", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading supplier: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Please enter supplier name.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierName.Focus();
                return;
            }

            if (cmbSupplierType.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updated = new Supplier
            {
                SupplierID = SupplierID,
                SupplierType = cmbSupplierType.SelectedItem?.ToString() ?? string.Empty, // must match enum
                SupplierName = txtSupplierName.Text.Trim(),
                SupplierPhone = string.IsNullOrWhiteSpace(txtSupplierPhone.Text) ? null : txtSupplierPhone.Text.Trim(),
                SupplierAddress = string.IsNullOrWhiteSpace(txtSupplierAddress.Text) ? null : txtSupplierAddress.Text.Trim(),
            };

            int result = 0;
            try
            {
                result = supplierManager.UpdateSupplier(updated);

                bool success = result != 0;
                if (!success)
                {
                    // Verifikasi: muat lagi untuk lihat apakah tersimpan
                    var reloaded = supplierManager.GetSupplierByID(SupplierID);
                    success =
                        reloaded != null &&
                        string.Equals(reloaded.SupplierName, updated.SupplierName, StringComparison.Ordinal) &&
                        string.Equals(reloaded.SupplierType, updated.SupplierType, StringComparison.Ordinal) &&
                        string.Equals(reloaded.SupplierPhone ?? string.Empty, updated.SupplierPhone ?? string.Empty, StringComparison.Ordinal) &&
                        string.Equals(reloaded.SupplierAddress ?? string.Empty, updated.SupplierAddress ?? string.Empty, StringComparison.Ordinal);
                }

                if (success)
                {
                    MessageBox.Show("Supplier updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    SupplierPage.NotifyDataChanged(); // trigger list reload
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this supplier?\nSupplier ID: {SupplierID}\nSupplier Name: {txtSupplierName.Text}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                int result = supplierManager.DeleteSupplier(SupplierID);

                bool success = result != 0;
                if (!success)
                {
                    var stillThere = supplierManager.GetSupplierByID(SupplierID);
                    success = (stillThere == null);
                }

                if (success)
                {
                    MessageBox.Show("Supplier deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    SupplierPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
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
                    MessageBox.Show($"Gagal memuat foto profil: {ex.Message}", "Error Foto", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}