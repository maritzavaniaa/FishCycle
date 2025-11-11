using FishCycleApp.DataAccess;
using FishCycleApp.Models;
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
        private string CurrentClientID;

        public ViewClientPage(string clientID)
        {
            InitializeComponent();
            this.CurrentClientID = clientID;
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
            this.NavigationService.Navigate(new EditClientPage(CurrentClientID));
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
    }
}