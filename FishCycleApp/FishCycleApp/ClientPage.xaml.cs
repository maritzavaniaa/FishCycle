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
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        private Person currentUserProfile;
        private ClientDataManager dataManager = new ClientDataManager();
        private DataView ClientDataView;
        private DataTable fullClientTable;

        public ClientPage(Person userProfile)
        {
            InitializeComponent();
            this.currentUserProfile = userProfile; // Simpan user profile
            DisplayProfileData(userProfile);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                fullClientTable = dataManager.LoadClientData();
                ClientDataView = fullClientTable.DefaultView;

                dgvClients.ItemsSource = ClientDataView;
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading client data: " + ex.Message, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateResultInfo()
        {
            if (ClientDataView != null)
            {
                int totalRecords = ClientDataView.Count;
                int displayedRecords = Math.Min(10, totalRecords);
                txtResultInfo.Text = $"showing 1-{displayedRecords} result from {totalRecords} results";
            }
        }

        // Search functionality
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search anything...";
                txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (searchText == "Search anything...")
                searchText = "";

            ApplyCombinedFilter(searchText);
        }

        // Category filter
        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...")
                searchText = "";

            ApplyCombinedFilter(searchText);
        }

        private void ApplyCombinedFilter(string searchText)
        {
            if (ClientDataView == null) return;

            try
            {
                string filterExpression = "";
                List<string> filters = new List<string>();

                // Search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    string search = searchText.Replace("'", "''"); // Escape single quotes
                    filters.Add($"(clientid LIKE '%{search}%' OR " +
                               $"client_name LIKE '%{search}%' OR " +
                               $"client_contact LIKE '%{search}%' OR " +
                               $"client_address LIKE '%{search}%')");
                }

                // Category filter
                var selectedCategory = (cmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(selectedCategory) &&
                    selectedCategory != "Category" &&
                    selectedCategory != "All Categories")
                {
                    filters.Add($"client_category = '{selectedCategory}'");
                }

                // Combine filters
                if (filters.Count > 0)
                {
                    filterExpression = string.Join(" AND ", filters);
                    ClientDataView.RowFilter = filterExpression;
                }
                else
                {
                    ClientDataView.RowFilter = null;
                }

                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Button events
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Pass currentUserProfile ke AddClientPage
            this.NavigationService.Navigate(new AddClientPage(this.currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            DataRowView selectedRow = button.DataContext as DataRowView;

            if (selectedRow != null)
            {
                string clientID = selectedRow["clientid"].ToString();
                // Pass clientID dan currentUserProfile
                this.NavigationService.Navigate(new ViewClientPage(clientID, this.currentUserProfile));
            }
            else
            {
                MessageBox.Show("Unable to retrieve client details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

                MessageBoxResult confirmation = MessageBox.Show(
                    $"Are you sure you want to delete Client '{clientName}'?",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

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