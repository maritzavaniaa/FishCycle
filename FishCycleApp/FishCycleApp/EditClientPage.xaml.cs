using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Xml.Linq;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for EditClientPage.xaml
    /// </summary>
    public partial class EditClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private string CurrentClientID;

        public EditClientPage(string clientID)
        {
            InitializeComponent();
            InitializeCategoryComboBox();
            this.CurrentClientID = clientID;
            LoadClientDetails(clientID);
            txtClientID.IsReadOnly = true;
        }

        private void InitializeCategoryComboBox()
        {
            cmbClientCategory.Items.Clear();
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Retail" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Restaurant" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Industry" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Distributor" });
        }

        private void LoadClientDetails(string clientID)
        {
            Client client = dataManager.GetClientByID(clientID);

            if (client != null)
            {
                txtClientID.Text = client.ClientID;
                txtClientName.Text = client.ClientName;
                txtClientContact.Text = client.ClientContact;
                cmbClientCategory.Text = client.ClientCategory;
                txtClientAddress.Text = client.ClientAddress;
            }
            else
            {
                MessageBox.Show($"ID Client {clientID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClientCategory.SelectedItem == null || string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                MessageBox.Show("Please select a client category.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Client updatedClient = new Client
            {
                ClientID = CurrentClientID,
                ClientName = txtClientName.Text,
                ClientContact = txtClientContact.Text,
                ClientAddress = txtClientAddress.Text,
                ClientCategory = ((ComboBoxItem)cmbClientCategory.SelectedItem).Content.ToString()
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

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show($"Are you sure you want to delete this client? Client ID: {CurrentClientID}", "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

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

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}