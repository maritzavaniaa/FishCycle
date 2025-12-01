using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Input;

namespace FishCycleApp
{
    public partial class EmployeePage : Page
    {
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person _currentUserProfile;
        private readonly EmployeeDataManager _dataManager = new EmployeeDataManager();

        private List<Employee> _allEmployees = new List<Employee>();
        private bool _isLoading;
        private DateTime _lastSuccessUtc;

        public EmployeePage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += EmployeePage_Loaded;
            this.Unloaded += EmployeePage_Unloaded;
            this.IsVisibleChanged += EmployeePage_IsVisibleChanged;
        }

        private async void EmployeePage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload || _allEmployees.Count == 0)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                ApplySearchFilter();
            }
        }

        private void EmployeePage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private async void EmployeePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
                var data = await _dataManager.LoadEmployeeDataAsync();

                if (data != null)
                {
                    _allEmployees = data;

                    _lastSuccessUtc = DateTime.UtcNow;

                    ApplySearchFilter();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmployeePage] Error: {ex.Message}");
                txtResultInfo.Text = "Error loading data.";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo(int count)
        {
            string suffix = _lastSuccessUtc != default ? $"" : string.Empty;
            txtResultInfo.Text = $"Total: {count} records found{suffix}";
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Employee data has been refreshed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Employee emp)
            {
                this.NavigationService.Navigate(new ViewEmployeePage(emp.EmployeeID, _currentUserProfile));
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddEmployeePage(_currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Employee emp)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete employee \"{emp.EmployeeName}\"?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    button.IsEnabled = false;
                    this.Cursor = Cursors.Wait;

                    bool success = await _dataManager.DeleteEmployeeAsync(emp.EmployeeID);

                    if (success)
                    {
                        MessageBox.Show("The employee has been deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                        NotifyDataChanged();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the employee. Please try again.", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                }
                finally
                {
                    button.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (dgvEmployees == null || _allEmployees == null) return;
            if (_allEmployees.Count == 0)
            {
                dgvEmployees.ItemsSource = null;
                UpdateResultInfo(0);
                return;
            }

            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...") searchText = string.Empty;

            IEnumerable<Employee> filteredData;

            if (string.IsNullOrEmpty(searchText))
            {
                filteredData = _allEmployees;
            }
            else
            {
                string lowerSearch = searchText.ToLower();

                filteredData = _allEmployees.Where(emp =>
                    (emp.EmployeeID != null && emp.EmployeeID.ToLower().Contains(lowerSearch)) ||
                    (emp.EmployeeName != null && emp.EmployeeName.ToLower().Contains(lowerSearch)) ||
                    (emp.GoogleAccount != null && emp.GoogleAccount.ToLower().Contains(lowerSearch))
                );
            }

            var resultList = filteredData.ToList();
            dgvEmployees.ItemsSource = resultList;

            UpdateResultInfo(resultList.Count);
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search anything...")
            {
                txtSearch.Text = string.Empty;
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

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                ? profile.Names[0].DisplayName
                : "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                string photoUrl = profile.Photos[0].Url;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch
                {
                }
            }
        }
    }
}