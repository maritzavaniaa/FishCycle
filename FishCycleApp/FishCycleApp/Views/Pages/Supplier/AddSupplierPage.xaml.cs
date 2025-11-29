using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class AddSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();

        public AddSupplierPage(Person userProfile)
        {
            InitializeComponent();
            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
        }

        private void InitializeCategoryComboBox()
        {
            cmbSupplierCategory.Items.Clear();
            // Content = tampilan di UI
            // Tag = ENUM value (harus persis dengan enum PostgreSQL)
            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "Fresh Catch", Tag = "Fresh Catch" });
            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "First-Hand", Tag = "First-Hand" });
            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "Reprocessed Stock", Tag = "Reprocessed Stock" });
            cmbSupplierCategory.SelectedIndex = 0;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Please enter supplier name.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierName.Focus();
                return;
            }

            if (cmbSupplierCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbSupplierCategory.SelectedItem;
            string supplierTypeEnum = selectedItem.Tag?.ToString() ?? selectedItem.Content.ToString();

            var newSupplier = new Supplier
            {
                SupplierID = "SID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                SupplierName = txtSupplierName.Text.Trim(),
                SupplierPhone = string.IsNullOrWhiteSpace(txtSupplierPhone.Text) ? null : txtSupplierPhone.Text.Trim(),
                SupplierAddress = string.IsNullOrWhiteSpace(txtSupplierAddress.Text) ? null : txtSupplierAddress.Text.Trim(),
                SupplierType = supplierTypeEnum
            };

            int result = 0;
            try
            {
                result = supplierManager.InsertSupplier(newSupplier);

                bool success = result != 0;
                if (!success)
                {
                    // Verifikasi: cek apakah supplier sudah tersimpan
                    var exists = supplierManager.GetSupplierByID(newSupplier.SupplierID);
                    success = exists != null;
                }

                if (success)
                {
                    MessageBox.Show("Supplier added successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    SupplierPage.NotifyDataChanged(); // trigger list reload
                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to add supplier.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Insert error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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