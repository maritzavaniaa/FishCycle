using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System.Threading.Tasks;

namespace FishCycleApp
{
    public partial class AddClientPage : Page
    {
        private readonly ClientDataManager dataManager = new ClientDataManager();
        private readonly Person currentUserProfile;
        private bool isSaving = false;   // anti double-click

        public AddClientPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
        }

        // ============================================================
        // INIT COMBOBOX — dibuat sama kaya SupplierPage
        // ============================================================
        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();

            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Retail", Tag = "Retail" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Restaurant", Tag = "Restaurant" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Industry", Tag = "Industry" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Distributor", Tag = "Distributor" });

            cmbClientCategory.SelectedIndex = 0;
        }

        // ============================================================
        // SAVE — disamakan strukturnya dengan AddSupplier
        // ============================================================
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

            // ---------------------
            // VALIDATION
            // ---------------------
            if (string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                MessageBox.Show("Please enter client name.", "WARNING");
                txtClientName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientContact.Text))
            {
                MessageBox.Show("Please enter client contact.", "WARNING");
                txtClientContact.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientAddress.Text))
            {
                MessageBox.Show("Please enter client address.", "WARNING");
                txtClientAddress.Focus();
                return;
            }

            if (cmbClientCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING");
                return;
            }

            try
            {
                // Lock UI (sama seperti AddSupplier)
                isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                // Ambil category (sama seperti supplier)
                var item = (ComboBoxItem)cmbClientCategory.SelectedItem;
                string category = item.Tag?.ToString() ?? item.Content.ToString();

                // ID generator (versi panjang aman)
                string clientID = "CID-" + DateTime.UtcNow.ToString("yyMMddHHmmssfff");

                var newClient = new Client
                {
                    ClientID = clientID,
                    ClientName = txtClientName.Text.Trim(),
                    ClientContact = txtClientContact.Text.Trim(),
                    ClientAddress = txtClientAddress.Text.Trim(),
                    ClientCategory = category
                };

                // Call async insert
                int result = await dataManager.InsertClientAsync(newClient);
                bool success = result != 0;

                // extra check bila DB return 0
                if (!success)
                {
                    var exists = await dataManager.GetClientByIDAsync(clientID);
                    success = exists != null;
                }

                if (success)
                {
                    MessageBox.Show("Client added successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClientPage.NotifyDataChanged();

                    // Back same as Supplier
                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                    else
                        NavigationService?.Navigate(new ClientPage(currentUserProfile));
                }
                else
                {
                    MessageBox.Show("Failed to add client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error:\n{ex.Message}", "EXCEPTION", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
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
        // USER PROFILE — sama dengan Supplier
        // ============================================================
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
                    bmp.UriSource = new Uri(profile.Photos[0].Url);
                    bmp.EndInit();
                    imgUserProfile.Source = bmp;
                }
                catch
                {
                    // silent fail — UX lebih baik
                }
            }
        }
    }
}
