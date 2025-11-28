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
        public static bool PendingReload { get; private set; }

        public static event Action? GlobalReloadRequested;

        public static void NotifyDataChanged()
        {
            PendingReload = true;
            GlobalReloadRequested?.Invoke();
        }

        public static event Action<Employee>? EmployeeDetailUpdated;
        public static Employee? LastUpdatedEmployee { get; private set; }

        public static void NotifyEmployeeUpdated(Employee updated)
        {
            LastUpdatedEmployee = updated;
            PendingReload = true;            
            EmployeeDetailUpdated?.Invoke(updated);
            GlobalReloadRequested?.Invoke();
        }

        private readonly Person currentUserProfile;
        private readonly EmployeeDataManager dataManager = new EmployeeDataManager();

        private DataView? EmployeeDataView;
        private DataTable? fullEmployeeTable;       
        private DataTable? lastSuccessfulTable;   
        private bool _isLoading;                   
        private DateTime _lastSuccessUtc;
        private bool _firstVisibilityHandled;      

        public EmployeePage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += EmployeePage_Loaded;
            this.Unloaded += EmployeePage_Unloaded;

            this.IsVisibleChanged += EmployeePage_IsVisibleChanged;

            _ = EnsureInitialLoadAsync();
        }

        private void EmployeePage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
            GlobalReloadRequested += OnGlobalReloadRequested;

            if (PendingReload)
            {
                PendingReload = false;
                LoadData();
                return;
            }

            if (lastSuccessfulTable != null)
            {
                EmployeeDataView = lastSuccessfulTable.DefaultView;
                dgvEmployees.ItemsSource = EmployeeDataView;
                UpdateResultInfo();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(async () => await TryLoadWithRetryAsync(2, 200)),
                                       DispatcherPriority.Background);
            }
        }

        private void EmployeePage_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalReloadRequested -= OnGlobalReloadRequested;
        }

        private void EmployeePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                if (!_firstVisibilityHandled)
                {
                    _firstVisibilityHandled = true;
                    return;
                }

                if (PendingReload)
                {
                    PendingReload = false;
                    LoadData();
                }
            }
        }

        private void OnGlobalReloadRequested()
        {
            Dispatcher.Invoke(async () => await TryLoadWithRetryAsync(2, 150));
        }

        private async Task EnsureInitialLoadAsync()
        {
            await Dispatcher.Yield(DispatcherPriority.Loaded);
            await TryLoadWithRetryAsync(3, 250);
        }

        private async Task TryLoadWithRetryAsync(int maxAttempts, int delayMs)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                LoadData();

                if (EmployeeDataView != null)
                    break;

                await Task.Delay(delayMs);
            }
        }

        private void LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var newTable = dataManager.LoadEmployeeData();
                fullEmployeeTable = newTable;

                if (newTable != null && newTable.Rows.Count > 0)
                {
                    lastSuccessfulTable = newTable.Copy();
                    _lastSuccessUtc = DateTime.UtcNow;

                    EmployeeDataView = newTable.DefaultView;
                    dgvEmployees.ItemsSource = EmployeeDataView;
                    UpdateResultInfo();
                }
                else
                {
                    if (lastSuccessfulTable != null)
                    {
                        EmployeeDataView = lastSuccessfulTable.DefaultView;
                        dgvEmployees.ItemsSource = EmployeeDataView;
                        UpdateResultInfo();
                        Console.WriteLine("Load returned empty; keeping last successful data.");
                    }
                    else
                    {
                        EmployeeDataView = null;
                        dgvEmployees.ItemsSource = null;
                        txtResultInfo.Text = "No employee data available";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading employee data: {ex.Message}");

                if (lastSuccessfulTable != null)
                {
                    EmployeeDataView = lastSuccessfulTable.DefaultView;
                    dgvEmployees.ItemsSource = EmployeeDataView;
                    UpdateResultInfo();
                }
                else
                {
                    txtResultInfo.Text = "Error loading data. Please refresh.";
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateResultInfo()
        {
            if (EmployeeDataView != null)
            {
                int totalRecords = EmployeeDataView.Count;
                int displayedRecords = Math.Min(10, totalRecords);
                string suffix = _lastSuccessUtc != default ? $" • last update {_lastSuccessUtc:HH:mm:ss}" : string.Empty;
                txtResultInfo.Text = $"showing 1-{displayedRecords} result from {totalRecords} results{suffix}";
            }
            else
            {
                txtResultInfo.Text = "No employee data available";
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

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmployeeDataView == null) return;

            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search anything...") searchText = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    EmployeeDataView.RowFilter = null;
                }
                else
                {
                    string search = searchText.Replace("'", "''");
                    string filterExpression =
                        $"employee_id LIKE '%{search}%' OR name LIKE '%{search}%' OR google_account LIKE '%{search}%'";
                    EmployeeDataView.RowFilter = filterExpression;
                }

                UpdateResultInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying filter: {ex.Message}");
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show("Data refreshed successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DataRowView row)
            {
                string employeeID = row["employee_id"].ToString();
                this.NavigationService.Navigate(new ViewEmployeePage(employeeID, currentUserProfile));
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddEmployeePage(currentUserProfile));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
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
                    int result = dataManager.DeleteEmployee(employeeID);

                    bool success = result != 0;
                    if (!success)
                    {
                        var stillThere = dataManager.GetEmployeeByID(employeeID);
                        success = (stillThere == null);
                    }

                    if (success)
                    {
                        MessageBox.Show("Employee deleted successfully.", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                        NotifyDataChanged(); 
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadData();
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to retrieve employee details.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load profile photo: {ex.Message}");
                }
            }
        }
    }
}