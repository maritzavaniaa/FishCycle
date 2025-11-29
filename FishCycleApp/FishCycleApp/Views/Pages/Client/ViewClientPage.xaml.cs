using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class ViewClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private Person currentUserProfile;
        private string CurrentClientID;
        private Client currentClient;

        public ViewClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            CurrentClientID = (clientID ?? string.Empty).Trim();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadClientDetails();
        }

        private void LoadClientDetails()
        {
            try
            {
                currentClient = dataManager.GetClientByID(CurrentClientID);
                if (currentClient != null)
                {
                    lblClientID.Text = currentClient.ClientID;
                    lblClientName.Text = currentClient.ClientName;
                    lblClientContact.Text = currentClient.ClientContact ?? "-";
                    lblClientCategory.Text = currentClient.ClientCategory ?? "-";
                    lblClientAddress.Text = currentClient.ClientAddress ?? "-";
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

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentClient == null)
            {
                MessageBox.Show("Client data not loaded.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService?.Navigate(new EditClientPage(CurrentClientID, currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentClientID))
            {
                MessageBox.Show("Invalid client ID.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this client?\nClient ID: {CurrentClientID}",
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
                    MessageBox.Show("Client deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClientPage.NotifyDataChanged();
                    NavigationService?.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadClientDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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