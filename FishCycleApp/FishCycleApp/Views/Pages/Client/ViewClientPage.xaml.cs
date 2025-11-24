using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for ViewClientPage.xaml
    /// </summary>
    public partial class ViewClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private Person currentUserProfile; // Field untuk menyimpan user profile
        private string CurrentClientID;

        // Constructor menerima clientID dan userProfile
        public ViewClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            this.CurrentClientID = clientID;
            this.currentUserProfile = userProfile; // Simpan user profile
            DisplayProfileData(userProfile); // Tampilkan profile data
            LoadClientDetails(clientID);
        }

        private void LoadClientDetails(string clientID)
        {
            Client client = dataManager.GetClientByID(clientID);
            if (client != null)
            {
                lblClientID.Text = client.ClientID;
                lblClientName.Text = client.ClientName;
                lblClientContact.Text = client.ClientContact;
                lblClientCategory.Text = client.ClientCategory;
                lblClientAddress.Text = client.ClientAddress;
            }
            else
            {
                MessageBox.Show($"ID Client {clientID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            // Pass clientID dan userProfile ke EditClientPage
            this.NavigationService.Navigate(new EditClientPage(CurrentClientID, this.currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this client?\nClient ID: {CurrentClientID}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = dataManager.DeleteClient(CurrentClientID);
                if (result == 1)
                {
                    MessageBox.Show("Client deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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