using System;
using System.Windows;
using System.Windows.Controls;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for AddClientPage.xaml
    /// </summary>
    public partial class AddClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();

        public AddClientPage()
        {
            InitializeComponent();
            InitializeCategoryComboBox();
        }

        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();

            // Content = yang keliatan di UI
            // Tag     = value ENUM di PostgreSQL (harus persis!)
            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Retail",
                Tag = "Retail"
            });

            cmbClientCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Restaurant",   // tampil di UI
                Tag = "Restoran"          // ENUM di DB (punyamu sekarang)
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
            if (cmbClientCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbClientCategory.SelectedItem;

            // ambil value ENUM untuk dikirim ke DB
            string categoryEnum = selectedItem.Tag?.ToString()
                                  ?? selectedItem.Content.ToString();

            Client newClient = new Client
            {
                ClientID = "CID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                ClientName = txtClientName.Text,
                ClientContact = txtClientContact.Text,
                ClientAddress = txtClientAddress.Text,
                ClientCategory = categoryEnum   // <- sudah cocok dengan enum DB
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
    }
}
