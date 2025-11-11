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
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Retail" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Restaurant" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Industry" });
            cmbClientCategory.Items.Add(new ComboBoxItem() { Content = "Distributor" });
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Client newClient = new Client
            {
                ClientID = "CID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                ClientName = txtClientName.Text,
                ClientContact = txtClientContact.Text,
                ClientAddress = txtClientAddress.Text,
                ClientCategory = ((ComboBoxItem)cmbClientCategory.SelectedItem).Content.ToString()
            };
            
            int result = dataManager.InsertClient(newClient);

            if (result == 1)
            {
                MessageBox.Show("Client added successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (this.NavigationService.CanGoBack)
                {
                    this.NavigationService.GoBack();
                }
            }
            else
            {
                MessageBox.Show("Failed to add client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}