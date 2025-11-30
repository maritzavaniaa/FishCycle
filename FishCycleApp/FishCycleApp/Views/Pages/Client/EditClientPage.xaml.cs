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
    public partial class EditClientPage : Page
    {
        private readonly ClientDataManager dataManager = new ClientDataManager();
        private readonly Person currentUserProfile;

        private Client WorkingClient;
        private bool isProcessing = false;

        public EditClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);
            InitializeCategory();

            _ = LoadClientByIdAsync(clientID);
        }

        public EditClientPage(Client client, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            WorkingClient = client;

            DisplayProfileData(userProfile);
            InitializeCategory();
            PopulateFieldsFromModel();
        }

        private void InitializeCategory()
        {
            cmbClientCategory.Items.Clear();
            cmbClientCategory.Items.Add("Retail");
            cmbClientCategory.Items.Add("Restaurant");
            cmbClientCategory.Items.Add("Industry");
            cmbClientCategory.Items.Add("Distributor");
        }

        private async Task LoadClientByIdAsync(string id)
        {
            try
            {
                Cursor = System.Windows.Input.Cursors.Wait;

                var c = await dataManager.GetClientByIDAsync(id?.Trim());
                if (c == null)
                {
                    Cursor = System.Windows.Input.Cursors.Arrow;
                    MessageBox.Show($"Client with ID {id} not found.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    NavigationService?.GoBack();
                    return;
                }

                WorkingClient = c;
                PopulateFieldsFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading client: {ex.Message}", "ERROR");
            }
            finally
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void PopulateFieldsFromModel()
        {
            if (WorkingClient == null) return;

            txtClientID.Text = WorkingClient.ClientID;
            txtClientName.Text = WorkingClient.ClientName;
            txtClientContact.Text = WorkingClient.ClientContact ?? "";
            txtClientAddress.Text = WorkingClient.ClientAddress ?? "";

            var match = cmbClientCategory.Items.Cast<object>()
                .FirstOrDefault(x => x.ToString() == WorkingClient.ClientCategory);

            cmbClientCategory.SelectedItem = match ?? WorkingClient.ClientCategory;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingClient == null || isProcessing) return;

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
                isProcessing = true;
                btnSave.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                WorkingClient.ClientName = txtClientName.Text.Trim();
                WorkingClient.ClientContact = txtClientContact.Text.Trim();
                WorkingClient.ClientAddress = txtClientAddress.Text.Trim();
                WorkingClient.ClientCategory = cmbClientCategory.SelectedItem.ToString();

                bool success = await dataManager.UpdateClientAsync(WorkingClient);

                if (!success)
                {
                    var reloaded = await dataManager.GetClientByIDAsync(WorkingClient.ClientID);
                    if (reloaded != null)
                    {
                        WorkingClient = reloaded;
                        success = true;
                    }
                }

                if (success)
                {
                    PopulateFieldsFromModel();
                    MessageBox.Show("Client updated successfully!", "SUCCESS");

                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update client.", "ERROR");
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
            if (WorkingClient == null || isProcessing) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete this client?\nID: {WorkingClient.ClientID}\nName: {WorkingClient.ClientName}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                isProcessing = true;
                btnDelete.IsEnabled = false;

                await dataManager.DeleteClientAsync(WorkingClient.ClientID);

                var stillExists = await dataManager.GetClientByIDAsync(WorkingClient.ClientID);
                bool success = stillExists == null;

                if (success)
                {
                    MessageBox.Show("Client deleted successfully!", "SUCCESS");
                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete client.", "ERROR");
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
                catch { }
            }
        }
    }
}