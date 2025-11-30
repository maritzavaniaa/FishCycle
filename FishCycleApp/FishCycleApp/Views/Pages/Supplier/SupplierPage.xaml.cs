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
using System.Linq;

namespace FishCycleApp
{
    public partial class SupplierPage : Page
    {
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person currentUserProfile;
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();

        private List<Supplier> _allSuppliers = new List<Supplier>(); private DateTime _lastSuccessUtc;
        private bool _isLoading;

        public SupplierPage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;

            DisplayProfileData(userProfile);

            this.Loaded += SupplierPage_Loaded;
            this.Unloaded += SupplierPage_Unloaded;
            this.IsVisibleChanged += SupplierPage_IsVisibleChanged;
        }

        private async void SupplierPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _allSuppliers.Count == 0)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                ApplyFilter(); 
            }
        }

        private void SupplierPage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void SupplierPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
                var data = await supplierManager.LoadSupplierDataAsync();

                if (data != null)
                {
                    _allSuppliers = data;
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
            txtResultInfo.Text = $"Total: {count} suppliers found{time}";
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
            if (dgvSuppliers == null || _allSuppliers == null) return;

            IEnumerable<Supplier> query = _allSuppliers;

            string search = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search) && search != "search anything...")
            {
                query = query.Where(s =>
                    (s.SupplierID != null && s.SupplierID.ToLower().Contains(search)) ||
                    (s.SupplierName != null && s.SupplierName.ToLower().Contains(search)) ||
                    (s.SupplierPhone != null && s.SupplierPhone.ToLower().Contains(search)) ||
                    (s.SupplierAddress != null && s.SupplierAddress.ToLower().Contains(search)) ||
                    (s.SupplierType != null && s.SupplierType.ToLower().Contains(search))
                );
            }

            if (cmbCategory.SelectedItem is ComboBoxItem item)
            {
                string cat = item.Content.ToString();
                if (cat != "All Categories" && cat != "Category")
                {
                    query = query.Where(s => s.SupplierType == cat);
                }
            }

            var resultList = query.ToList();
            dgvSuppliers.ItemsSource = resultList;
            UpdateResultInfo(resultList.Count);
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddSupplierPage(currentUserProfile));
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Supplier s)
            {
                this.NavigationService.Navigate(new ViewSupplierPage(s.SupplierID, currentUserProfile));
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Supplier s)
            {
                var confirm = MessageBox.Show($"Delete {s.SupplierName}?", "CONFIRM", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                await supplierManager.DeleteSupplierAsync(s.SupplierID);

                MessageBox.Show("Deleted!");
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