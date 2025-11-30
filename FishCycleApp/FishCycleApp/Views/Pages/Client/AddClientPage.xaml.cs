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
        private bool isSaving = false; 

        public AddClientPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
        }

        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();

            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Retail", Tag = "Retail" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Restaurant", Tag = "Restaurant" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Industry", Tag = "Industry" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Distributor", Tag = "Distributor" });

            cmbClientCategory.SelectedIndex = 0;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

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
                isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                var item = (ComboBoxItem)cmbClientCategory.SelectedItem;
                string category = item.Tag?.ToString() ?? item.Content.ToString();

                string clientID = "CID-" + DateTime.UtcNow.ToString("yyMMddHHmmss");

                var newClient = new Client
                {
                    ClientID = clientID,
                    ClientName = txtClientName.Text.Trim(),
                    ClientContact = txtClientContact.Text.Trim(),
                    ClientAddress = txtClientAddress.Text.Trim(),
                    ClientCategory = category
                };

                bool success = await dataManager.InsertClientAsync(newClient);

                if (!success)
                {
                    var exists = await dataManager.GetClientByIDAsync(clientID);
                    success = exists != null;
                }

                if (success)
                {
                    MessageBox.Show("Client added successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClientPage.NotifyDataChanged();

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

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
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
                    bmp.UriSource = new Uri(profile.Photos[0].Url);
                    bmp.EndInit();
                    imgUserProfile.Source = bmp;
                }
                catch
                {
                   
                }
            }
        }
    }
}