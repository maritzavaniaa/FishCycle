using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class AddSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();
        private readonly Person currentUserProfile;
        private bool isSaving = false; // Anti double-click

        public AddSupplierPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
        }

        // ============================================================
        // ComboBox Initialization
        // ============================================================
        private void InitializeCategoryComboBox()
        {
            cmbSupplierCategory.Items.Clear();

            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "Fresh Catch", Tag = "Fresh Catch" });
            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "First-Hand", Tag = "First-Hand" });
            cmbSupplierCategory.Items.Add(new ComboBoxItem { Content = "Reprocessed Stock", Tag = "Reprocessed Stock" });

            cmbSupplierCategory.SelectedIndex = 0;
        }

        // ============================================================
        // SAVE BUTTON — ASYNC & MIRIP AddEmployeePage versi optimal
        // ============================================================
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

            // Validation
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Please enter supplier name.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSupplierName.Focus();
                return;
            }

            if (cmbSupplierCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Lock UI
                isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                var categoryItem = (ComboBoxItem)cmbSupplierCategory.SelectedItem;
                string supplierType = categoryItem.Tag?.ToString() ?? categoryItem.Content.ToString();

                var newSupplier = new Supplier
                {
                    SupplierID = "SID-" + DateTime.UtcNow.ToString("yyMMddHHmmss"),
                    SupplierName = txtSupplierName.Text.Trim(),
                    SupplierPhone = string.IsNullOrWhiteSpace(txtSupplierPhone.Text) ? null : txtSupplierPhone.Text.Trim(),
                    SupplierAddress = string.IsNullOrWhiteSpace(txtSupplierAddress.Text) ? null : txtSupplierAddress.Text.Trim(),
                    SupplierType = supplierType
                };

                // Async insert
                int result = await supplierManager.InsertSupplierAsync(newSupplier);

                bool success = result != 0;

                // Double check if DB returns affected = 0
                if (!success)
                {
                    var exists = await supplierManager.GetSupplierByIDAsync(newSupplier.SupplierID);
                    success = exists != null;
                }

                if (success)
                {
                    MessageBox.Show("Supplier added successfully!", "SUCCESS",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Notify SupplierPage to reload list
                    SupplierPage.NotifyDataChanged();

                    // Proper navigation (consistent with AddEmployeePage)
                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                    else
                        NavigationService?.Navigate(new SupplierPage(currentUserProfile));
                }
                else
                {
                    MessageBox.Show("Failed to add supplier.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error:\n{ex.Message}", "EXCEPTION",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // unlock
                isSaving = false;
                btnSave.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        // ============================================================
        // CANCEL
        // ============================================================
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        // ============================================================
        // USER PROFILE (Identical to Employee)
        // ============================================================
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
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bmp.EndInit();

                    imgUserProfile.Source = bmp;
                }
                catch
                {
                    // ignored — better UX than popup
                }
            }
        }
    }
}
