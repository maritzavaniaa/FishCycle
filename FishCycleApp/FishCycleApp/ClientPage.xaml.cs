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
using System.ComponentModel;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        private ClientDataManager dataManager = new ClientDataManager();
        private DataView ClientDataView;

        public ClientPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                DataTable clientTable = dataManager.LoadClientData();

                ClientDataView = clientTable.DefaultView;

                dgvClients.ItemsSource = ClientDataView;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading client data: " + ex.Message, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string clientID = selectedRow["clientid"].ToString();

                this.NavigationService.Navigate(new ViewClientPage(clientID));
            }
            else
            {
                MessageBox.Show("Unable to retrieve client details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddClientPage());
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            { 
                string clientID = selectedRow["clientid"].ToString();
                string clientName = selectedRow["client_name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show($"Are you sure you want to delete Client Name {clientName}?", "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (confirmation == MessageBoxResult.Yes)
                {
                    int result = dataManager.DeleteClient(clientID);

                    if (result == 1)
                    {
                        MessageBox.Show("Client deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete client.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to retrieve client details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClientPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible == true)
            {
                LoadData();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClientDataView == null) return;

            string searchText = (sender as TextBox).Text.Trim();
            string filterExpression = "";

            if (string.IsNullOrEmpty(searchText))
            {
                ClientDataView.RowFilter = null;
            }
            else
            {
                string search = $"%{searchText}%";

                filterExpression = $"clientid LIKE '{search}' OR " +
                                   $"client_name LIKE '{search}' OR " +
                                   $"client_contact LIKE '{search}' OR " +
                                   $"client_address LIKE '{search}' OR " +
                                   $"client_category LIKE '{search}' ";

                ClientDataView.RowFilter = filterExpression;
            }
        }
    }
}