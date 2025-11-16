using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for AddClientPage.xaml
    /// </summary>
    public partial class AddClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();

        public AddClientPage(Person userProfile)
        {
            InitializeComponent();
            DisplayProfileData(userProfile);
            InitializeCategoryComboBox();
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
            // optional: pilih default
            cmbClientCategory.SelectedIndex = 0;
        }

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

            Client newClient = new Client
            {
                ClientID = "CID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                ClientName = txtClientName.Text.Trim(),
                ClientContact = txtClientContact.Text.Trim(),
                ClientAddress = txtClientAddress.Text.Trim(),
                ClientCategory = categoryEnum
            };

            int result = dataManager.InsertClient(newClient);

            if (result == 1)
            {
                MessageBox.Show("Client added successfully!",
                                "SUCCESS",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                if (this.NavigationService?.CanGoBack == true)
                {
                    this.NavigationService.GoBack();
                }
            }
            else
            {
                MessageBox.Show("Failed to add client.",
                                "ERROR",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
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