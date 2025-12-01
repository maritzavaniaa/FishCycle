using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
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
                MessageBox.Show("Please enter the client name.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientContact.Text))
            {
                MessageBox.Show("Please enter the client contact information.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientContact.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientAddress.Text))
            {
                MessageBox.Show("Please enter the client address.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientAddress.Focus();
                return;
            }

            if (cmbClientCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a client category.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("Client information has been updated successfully.", "Update Successful",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update client. Connection might be lost.", "Update Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                $"Are you sure you want to delete this client?\n\nID: {WorkingClient.ClientID}\nName: {WorkingClient.ClientName}",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

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
                    MessageBox.Show("The client has been deleted successfully.", "Delete Successful", 
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Unable to delete the client. Please try again.",
                        "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);

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