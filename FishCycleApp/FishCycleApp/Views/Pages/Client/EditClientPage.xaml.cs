using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class EditClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private Person currentUserProfile;
        private string CurrentClientID;
        private Client currentClient;

        public EditClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            CurrentClientID = (clientID ?? string.Empty).Trim();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
            LoadClientDetails();
        }

        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Retail", Tag = "Retail" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Restaurant", Tag = "Restaurant" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Industry", Tag = "Industry" });
            cmbClientCategory.Items.Add(new ComboBoxItem { Content = "Distributor", Tag = "Distributor" });
        }

        private void LoadClientDetails()
        {
            try
            {
                currentClient = dataManager.GetClientByID(CurrentClientID);
                if (currentClient != null)
                {
                    txtClientID.Text = currentClient.ClientID;
                    txtClientName.Text = currentClient.ClientName;
                    txtClientContact.Text = currentClient.ClientContact ?? string.Empty;
                    txtClientAddress.Text = currentClient.ClientAddress ?? string.Empty;

                    var item = cmbClientCategory.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => string.Equals(i.Tag?.ToString(), currentClient.ClientCategory, StringComparison.Ordinal));
                    if (item != null) cmbClientCategory.SelectedItem = item;
                }
                else
                {
                    MessageBox.Show($"Client with ID {CurrentClientID} not found.", "NOT FOUND", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading client: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                MessageBox.Show("Please enter client name.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientContact.Text))
            {
                MessageBox.Show("Please enter client contact.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientContact.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientAddress.Text))
            {
                MessageBox.Show("Please enter client address.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientAddress.Focus();
                return;
            }

            if (cmbClientCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbClientCategory.SelectedItem;
            string categoryEnum = selectedItem.Tag?.ToString() ?? selectedItem.Content.ToString();

            Client updatedClient = new Client
            {
                ClientID = CurrentClientID,
                ClientName = txtClientName.Text.Trim(),
                ClientContact = txtClientContact.Text.Trim(),
                ClientAddress = txtClientAddress.Text.Trim(),
                ClientCategory = categoryEnum
            };

            try
            {
                int result = dataManager.UpdateClient(updatedClient);
                bool success = result != 0;

                if (!success)
                {
                    var reloaded = dataManager.GetClientByID(CurrentClientID);
                    success =
                        reloaded != null &&
                        string.Equals(reloaded.ClientName, updatedClient.ClientName, StringComparison.Ordinal) &&
                        string.Equals(reloaded.ClientContact, updatedClient.ClientContact, StringComparison.Ordinal) &&
                        string.Equals(reloaded.ClientAddress, updatedClient.ClientAddress, StringComparison.Ordinal) &&
                        string.Equals(reloaded.ClientCategory, updatedClient.ClientCategory, StringComparison.Ordinal);
                }

                if (success)
                {
                    MessageBox.Show("Client updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
                $"Are you sure you want to delete this client?\nClient ID: {CurrentClientID}\nClient Name: {txtClientName.Text}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                int result = dataManager.DeleteClient(CurrentClientID);
                bool success = result != 0;

                if (!success)
                {
                    var stillThere = dataManager.GetClientByID(CurrentClientID);
                    success = (stillThere == null);
                }

                if (success)
                {
                    MessageBox.Show("Client deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Console.WriteLine($"Failed to load profile photo: {ex.Message}");
                }
            }
        }
    }
}