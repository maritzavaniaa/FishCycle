using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FishCycleApp
{
    public partial class EmployeePage : Page
    {
        // ==========================================
        // STATICS / EVENTS
        // ==========================================
        public static bool PendingReload { get; private set; }
        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        // ==========================================
        // FIELDS
        // ==========================================
        private readonly Person _currentUserProfile;
        private readonly EmployeeDataManager _dataManager = new EmployeeDataManager();

        private DataView? _employeeDataView;
        private bool _isLoading;
        private DateTime _lastSuccessUtc;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public EmployeePage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += EmployeePage_Loaded;
            this.Unloaded += EmployeePage_Unloaded;
            this.IsVisibleChanged += EmployeePage_IsVisibleChanged;
        }

        // ==========================================
        // LIFECYCLE EVENTS
        // ==========================================
        private async void EmployeePage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            // Jika ada pending reload atau data belum pernah dimuat
            if (PendingReload || _employeeDataView == null)
            {
                PendingReload = false;
                await LoadDataAsync();
            }
            else
            {
                // Jika data sudah ada di memori, refresh tampilan saja
                dgvEmployees.ItemsSource = _employeeDataView;
                UpdateResultInfo();
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
            // Pastikan berjalan di UI Thread
            await Dispatcher.InvokeAsync(async () => await LoadDataAsync());
        }

        // ==========================================
        // DATA LOADING (CORE LOGIC)
        // ==========================================
        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;
                // Opsional: Tampilkan Loading Spinner jika ada, misal: LoadingBar.Visibility = Visibility.Visible;

                // 1. Panggil method Async dari DataManager (Tidak akan memblokir UI)
                DataTable newTable = await _dataManager.LoadEmployeeDataAsync();

                if (newTable != null)
                {
                    _employeeDataView = newTable.DefaultView;
                    dgvEmployees.ItemsSource = _employeeDataView;

                    _lastSuccessUtc = DateTime.UtcNow;
                    UpdateResultInfo();

                    // Re-apply filter jika ada teks di kolom pencarian
                    ApplySearchFilter();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmployeePage] Error loading data: {ex.Message}");
                txtResultInfo.Text = "Error loading data. Please refresh.";
            }
            finally
            {
                _isLoading = false;
                // LoadingBar.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateResultInfo()
        {
            if (_employeeDataView != null)
            {
                int totalRecords = _employeeDataView.Count; // Count rows after filter
                int displayedRecords = Math.Min(10, totalRecords); // Just logic display
                string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
                txtResultInfo.Text = $"Total: {totalRecords} records found{suffix}";
            }
            else
            {
                txtResultInfo.Text = "No employee data available";
            }
        }

        // ==========================================
        // USER INTERACTIONS (BUTTONS)
        // ==========================================
        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string employeeID = row["employee_id"].ToString();
                this.NavigationService.Navigate(new ViewEmployeePage(employeeID, _currentUserProfile));
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddEmployeePage(_currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string employeeID = row["employee_id"].ToString();
                string employeeName = row["name"].ToString();

                MessageBoxResult confirmation = MessageBox.Show(
                    $"Are you sure you want to delete Employee Name {employeeName}?",
                    "CONFIRM DELETE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Panggil Method Delete Async
                        int result = await _dataManager.DeleteEmployeeAsync(employeeID);

                        if (result != 0)
                        {
                            MessageBox.Show("Employee deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                            NotifyDataChanged(); // Beritahu halaman lain
                            await LoadDataAsync(); // Reload halaman ini
                        }
                        else
                        {
                            // Double check: Cek apakah user sudah hilang?
                            var exists = await _dataManager.GetEmployeeByIDAsync(employeeID);
                            if (exists == null)
                            {
                                // Sudah terhapus sebenarnya
                                await LoadDataAsync();
                            }
                            else
                            {
                                MessageBox.Show("Failed to delete employee. Database returned 0 rows affected.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting employee: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ==========================================
        // SEARCH & FILTER LOGIC
        // ==========================================
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (_employeeDataView == null) return;

            string searchText = txtSearch.Text.Trim();

            // Handle placeholder text
            if (searchText == "Search anything...") searchText = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    _employeeDataView.RowFilter = null;
                }
                else
                {
                    // Escape karakter petik satu (') agar tidak error syntax SQL/RowFilter
                    string search = searchText.Replace("'", "''");

                    _employeeDataView.RowFilter =
                        $"employee_id LIKE '%{search}%' OR " +
                        $"name LIKE '%{search}%' OR " +
                        $"google_account LIKE '%{search}%'";
                }
                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Filter Error: {ex.Message}");
            }
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

        // ==========================================
        // UI HELPERS
        // ==========================================
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
                    // Ignore image error
                }
            }
        }
    }
}