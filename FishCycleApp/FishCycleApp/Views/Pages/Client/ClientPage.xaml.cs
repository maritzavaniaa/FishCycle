using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class ClientPage : Page
    {
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person currentUserProfile;
        private readonly ClientDataManager dataManager = new ClientDataManager();

        private List<Client> _allClients = new List<Client>(); private bool _isLoading;
        private DateTime _lastSuccessUtc;

        public ClientPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);

            this.Loaded += ClientPage_Loaded;
            this.Unloaded += ClientPage_Unloaded;
            this.IsVisibleChanged += ClientPage_IsVisibleChanged;
        }

        private async void ClientPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _allClients.Count == 0)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                ApplyFilter();
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

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            try
            {
                _isLoading = true;
                var data = await dataManager.LoadClientDataAsync();

                if (data != null)
                {
                    _allClients = data;
                    _lastSuccessUtc = DateTime.UtcNow;
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo(int count)
        {
            string time = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : "";
            txtResultInfo.Text = $"Total: {count} clients found{time}";
        }

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
            if (dgvClients == null || _allClients == null) return;

            IEnumerable<Client> query = _allClients;

            string search = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search) && search != "search anything...")
            {
                query = query.Where(c =>
                    (c.ClientID != null && c.ClientID.ToLower().Contains(search)) ||
                    (c.ClientName != null && c.ClientName.ToLower().Contains(search)) ||
                    (c.ClientContact != null && c.ClientContact.ToLower().Contains(search)) ||
                    (c.ClientAddress != null && c.ClientAddress.ToLower().Contains(search)) ||
                    (c.ClientCategory != null && c.ClientCategory.ToLower().Contains(search))
                );
            }

            if (cmbCategory.SelectedItem is ComboBoxItem item)
            {
                string cat = item.Content.ToString();
                if (cat != "All Categories" && cat != "Category")
                {
                    query = query.Where(c => c.ClientCategory == cat);
                }
            }

            var resultList = query.ToList();
            dgvClients.ItemsSource = resultList;
            UpdateResultInfo(resultList.Count);
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Client data has been refreshed successfully.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddClientPage(currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Client c)
            {
                this.NavigationService.Navigate(new ViewClientPage(c.ClientID, currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Client c)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete client \"{c.ClientName}\"?", "Confirm Deletion",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                await dataManager.DeleteClientAsync(c.ClientID);

                MessageBox.Show("Client has been deleted successfully.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NotifyDataChanged();
                await LoadDataAsync();
            }
        }

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