using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class EditClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private Person currentUserProfile;
        private string CurrentClientID;

        public EditClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            this.CurrentClientID = clientID;
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
            LoadClientDetails(clientID);
        }

        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();
            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Retail",
                Tag = "Retail"
            });
            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Restaurant",
                Tag = "Restaurant"
            });
            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Industry",
                Tag = "Industry"
            });
            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Distributor",
                Tag = "Distributor"
            });
        }

        // Fungsi untuk memuat detail client berdasarkan ID
        private void LoadClientDetails(string clientID)
        {
            Client client = dataManager.GetClientByID(clientID);
            if (client != null)
            {
                txtClientID.Text = client.ClientID;
                txtClientName.Text = client.ClientName;
                txtClientContact.Text = client.ClientContact;
                txtClientAddress.Text = client.ClientAddress;

                // Set category di ComboBox
                foreach (ComboBoxItem item in cmbClientCategory.Items)
                {
                    if (item.Tag.ToString() == client.ClientCategory)
                    {
                        cmbClientCategory.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show($"Client with ID {clientID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        // Fungsi untuk menyimpan perubahan data client
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input kosong
            if (string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                MessageBox.Show("Please enter client name.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientContact.Text))
            {
                MessageBox.Show("Please enter client contact.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientContact.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientAddress.Text))
            {
                MessageBox.Show("Please enter client address.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientAddress.Focus();
                return;
            }

            if (cmbClientCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbClientCategory.SelectedItem;
            string categoryEnum = selectedItem.Tag?.ToString()
                                  ?? selectedItem.Content.ToString();

            Client updatedClient = new Client
            {
                ClientID = CurrentClientID,
                ClientName = txtClientName.Text.Trim(),
                ClientContact = txtClientContact.Text.Trim(),
                ClientAddress = txtClientAddress.Text.Trim(),
                ClientCategory = categoryEnum
            };

            int result = dataManager.UpdateClient(updatedClient);

            if (result == 1)
            {
                MessageBox.Show("Client updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                this.NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fungsi untuk menghapus data client
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this client?\nClient ID: {CurrentClientID}\nClient Name: {txtClientName.Text}",
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

        // Fungsi untuk kembali ke halaman sebelumnya
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
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