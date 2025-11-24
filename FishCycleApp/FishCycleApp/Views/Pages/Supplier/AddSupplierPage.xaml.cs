using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class AddSupplierPage : Page
    {
        private SupplierDataManager supplierManager = new SupplierDataManager();

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
            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Fresh Catch",
                Tag = "Fresh Catch"
            });
            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "First-Hand",
                Tag = "First-Hand"
            });
            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Reprocessed Stock",
                Tag = "Reprocessed Stock"
            });
            cmbSupplierCategory.SelectedIndex = 0; // optional
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

            if (cmbSupplierCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbSupplierCategory.SelectedItem;
            string supplierTypeEnum = selectedItem.Tag?.ToString()
                                      ?? selectedItem.Content.ToString();

            Supplier newSupplier = new Supplier
            {
                SupplierID = "SID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                SupplierName = txtSupplierName.Text.Trim(),
                SupplierPhone = txtSupplierPhone.Text.Trim(),
                SupplierAddress = txtSupplierAddress.Text.Trim(),
                SupplierType = supplierTypeEnum  // ENUM aman
            };

            int result = supplierManager.InsertSupplier(newSupplier);

            if (result == 1)
            {
                MessageBox.Show("Supplier added successfully!",
                    "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to add supplier.",
                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
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