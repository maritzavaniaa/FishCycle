using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class EditSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();
        private readonly Person currentUserProfile;
        private Supplier WorkingSupplier;
        private bool isProcessing = false;

        public EditSupplierPage(Supplier supplier, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            WorkingSupplier = supplier;

            DisplayProfileData(userProfile);
            InitializeCategory();
            PopulateFieldsFromModel();
        }

        public EditSupplierPage(string supplierID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);
            InitializeCategory();

            _ = LoadSupplierByIdAsync(supplierID);
        }

        private void InitializeCategory()
        {
            cmbSupplierType.Items.Clear();
            cmbSupplierType.Items.Add("Fresh Catch");
            cmbSupplierType.Items.Add("First-Hand");
            cmbSupplierType.Items.Add("Reprocessed Stock");
        }

        private async Task LoadSupplierByIdAsync(string id)
        {
            try
            {
                Cursor = System.Windows.Input.Cursors.Wait;

                var s = await supplierManager.GetSupplierByIDAsync(id?.Trim());
                if (s == null)
                {
                    Cursor = System.Windows.Input.Cursors.Arrow;
                    MessageBox.Show($"Supplier with ID {id} not found.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService?.GoBack();
                    return;
                }

                WorkingSupplier = s;
                PopulateFieldsFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading supplier: {ex.Message}", "ERROR");
            }
            finally
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void PopulateFieldsFromModel()
        {
            if (WorkingSupplier == null) return;

            txtSupplierID.Text = WorkingSupplier.SupplierID;
            txtSupplierName.Text = WorkingSupplier.SupplierName;
            txtSupplierPhone.Text = WorkingSupplier.SupplierPhone ?? "";
            txtSupplierAddress.Text = WorkingSupplier.SupplierAddress ?? "";

            var match = cmbSupplierType.Items.Cast<object>()
                .FirstOrDefault(x => x.ToString() == WorkingSupplier.SupplierType);

            cmbSupplierType.SelectedItem = match ?? WorkingSupplier.SupplierType;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingSupplier == null || isProcessing) return;

            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Please enter supplier name.", "WARNING");
                txtSupplierName.Focus();
                return;
            }

            if (cmbSupplierType.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING");
                return;
            }

            try
            {
                isProcessing = true;
                btnSave.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                WorkingSupplier.SupplierName = txtSupplierName.Text.Trim();
                WorkingSupplier.SupplierType = cmbSupplierType.SelectedItem.ToString();
                WorkingSupplier.SupplierPhone = string.IsNullOrWhiteSpace(txtSupplierPhone.Text) ? null : txtSupplierPhone.Text.Trim();
                WorkingSupplier.SupplierAddress = string.IsNullOrWhiteSpace(txtSupplierAddress.Text) ? null : txtSupplierAddress.Text.Trim();

                int result = await supplierManager.UpdateSupplierAsync(WorkingSupplier);

                bool success = result != 0;

                if (!success)
                {
                    var reloaded = await supplierManager.GetSupplierByIDAsync(WorkingSupplier.SupplierID);
                    if (reloaded != null)
                    {
                        WorkingSupplier = reloaded;
                        success = true;
                    }
                }

                if (success)
                {
                    PopulateFieldsFromModel();
                    MessageBox.Show("Supplier updated successfully!", "SUCCESS");

                    SupplierPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update supplier.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Error: {ex.Message}", "ERROR");
            }
            finally
            {
                isProcessing = false;
                btnSave.IsEnabled = true;
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingSupplier == null || isProcessing) return;

            var id = WorkingSupplier.SupplierID;
            var name = WorkingSupplier.SupplierName;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete this supplier?\nID: {id}\nName: {name}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                isProcessing = true;
                btnDelete.IsEnabled = false;

                await supplierManager.DeleteSupplierAsync(id);

                var stillExists = await supplierManager.GetSupplierByIDAsync(id);
                bool success = stillExists == null;

                if (success)
                {
                    MessageBox.Show("Supplier deleted successfully!", "SUCCESS");
                    SupplierPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete supplier.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}", "ERROR");
            }
            finally
            {
                isProcessing = false;
                btnDelete.IsEnabled = true;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile.Names?.FirstOrDefault()?.DisplayName ?? "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(profile.Photos[0].Url);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch { }
            }
        }
    }
}
