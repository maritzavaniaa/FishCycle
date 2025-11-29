using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class ClientPage : Page
    {
        // ============================================================
        // GLOBAL RELOAD
        // ============================================================
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        // ============================================================
        // FIELDS
        // ============================================================
        private readonly Person currentUserProfile;
        private readonly ClientDataManager dataManager = new ClientDataManager();

        private DataView? ClientDataView;
        private bool _isLoading;
        private DateTime _lastSuccessUtc;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        public ClientPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);

            this.Loaded += ClientPage_Loaded;
            this.Unloaded += ClientPage_Unloaded;
            this.IsVisibleChanged += ClientPage_IsVisibleChanged;
        }

        // ============================================================
        // PAGE LIFECYCLE — IDENTIK DENGAN SupplierPage
        // ============================================================
        private async void ClientPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || ClientDataView == null)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                dgvClients.ItemsSource = ClientDataView;
                UpdateResultInfo();
            }
        }

        private void ClientPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void ClientPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && PendingReload)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
        }

        private async void OnGlobalReloadRequested()
        {
            await Dispatcher.InvokeAsync(async () => await LoadDataAsync());
        }

        // ============================================================
        // LOAD DATA (robust & identik dengan SupplierPage)
        // ============================================================
        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;

                DataTable dt = await dataManager.LoadClientDataAsync();
                ClientDataView = dt.DefaultView;

                dgvClients.ItemsSource = ClientDataView;

                _lastSuccessUtc = DateTime.UtcNow;

                ApplyFilter();
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                txtResultInfo.Text = "Error loading client data.";
                Console.WriteLine($"[ClientPage] Load error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo()
        {
            if (ClientDataView != null)
            {
                int total = ClientDataView.Count;
                string time = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : "";

                txtResultInfo.Text = $"Total: {total} clients found{time}";
            }
            else
            {
                txtResultInfo.Text = "No client data available";
            }
        }

        // ============================================================
        // SEARCH + FILTER (identik formatnya dengan Supplier)
        // ============================================================
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (ClientDataView == null) return;

            List<string> filters = new List<string>();

            // text search
            string search = txtSearch.Text.Trim();
            if (search == "Search anything...") search = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Replace("'", "''");

                filters.Add(
                    $"clientid LIKE '%{s}%' OR " +
                    $"client_name LIKE '%{s}%' OR " +
                    $"client_contact LIKE '%{s}%' OR " +
                    $"client_address LIKE '%{s}%' OR " +
                    $"client_category LIKE '%{s}%'"
                );
            }

            // category filter
            if (cmbCategory.SelectedItem is ComboBoxItem item)
            {
                string cat = item.Content.ToString();
                if (cat != "All Categories" && cat != "Category")
                {
                    string esc = cat.Replace("'", "''");
                    filters.Add($"client_category = '{esc}'");
                }
            }

            ClientDataView.RowFilter =
                filters.Count > 0 ? string.Join(" AND ", filters) : "";

            UpdateResultInfo();
        }

        // ============================================================
        // BUTTONS
        // ============================================================
        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!",
                "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddClientPage(currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                string id = row["clientid"].ToString();
                this.NavigationService.Navigate(new ViewClientPage(id, currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not DataRowView row)
                return;

            string id = row["clientid"].ToString();
            string name = row["client_name"].ToString();

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete Client \"{name}\"?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            await dataManager.DeleteClientAsync(id);

            var stillExist = await dataManager.GetClientByIDAsync(id);
            bool success = stillExist == null;

            if (success)
            {
                MessageBox.Show("Client deleted successfully.",
                    "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                NotifyDataChanged();
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show("Failed to delete client.",
                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // PROFILE
        // ============================================================
        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "User";

            try
            {
                if (profile?.Photos?.Count > 0)
                {
                    var bm = new BitmapImage();
                    bm.BeginInit();
                    bm.CacheOption = BitmapCacheOption.OnLoad;
                    bm.UriSource = new Uri(profile.Photos[0].Url);
                    bm.EndInit();
                    imgUserProfile.Source = bm;
                }
            }
            catch { }
        }

        // ============================================================
        // Placeholder Behavior
        // ============================================================
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
                txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Search anything...";
        }
    }
}
