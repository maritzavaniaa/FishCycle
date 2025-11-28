using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace FishCycleApp
{
    public partial class ViewEmployeePage : Page
    {
        private readonly EmployeeDataManager dataManager = new EmployeeDataManager();
        private readonly Person currentUserProfile;
        private Employee LoadedEmployee;

        public ViewEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            this.Loaded += ViewEmployeePage_Loaded;
            this.Unloaded += ViewEmployeePage_Unloaded;

            LoadEmployeeDetails(employeeID?.Trim());
        }

        private void ViewEmployeePage_Loaded(object sender, RoutedEventArgs e)
        {
            EmployeePage.EmployeeDetailUpdated -= OnEmployeeUpdated;
            EmployeePage.EmployeeDetailUpdated += OnEmployeeUpdated;

            var pending = EmployeePage.LastUpdatedEmployee;
            if (pending != null && LoadedEmployee != null &&
                string.Equals(pending.EmployeeID, LoadedEmployee.EmployeeID, StringComparison.OrdinalIgnoreCase))
            {
                ApplyToUI(pending);
                LoadedEmployee = pending;
            }
        }

        private void ViewEmployeePage_Unloaded(object sender, RoutedEventArgs e)
        {
            EmployeePage.EmployeeDetailUpdated -= OnEmployeeUpdated;
        }

        private void OnEmployeeUpdated(Employee updated)
        {
            if (LoadedEmployee != null &&
                string.Equals(updated.EmployeeID, LoadedEmployee.EmployeeID, StringComparison.OrdinalIgnoreCase))
            {
                LoadedEmployee = updated;
                ApplyToUI(updated);
            }
        }

        private void LoadEmployeeDetails(string employeeID)
        {
            LoadedEmployee = dataManager.GetEmployeeByID(employeeID);

            if (LoadedEmployee == null)
            {
                var suffix = employeeID?
                    .Replace("EMP-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("EID-", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (!string.IsNullOrEmpty(suffix))
                {
                    LoadedEmployee = dataManager.GetEmployeeByID("EMP-" + suffix)
                                    ?? dataManager.GetEmployeeByID("EID-" + suffix);
                }
            }

            if (LoadedEmployee != null)
            {
                ApplyToUI(LoadedEmployee);
            }
            else
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateToEmployeeList();
            }
        }

        private void ApplyToUI(Employee e)
        {
            lblEmployeeID.Text = e.EmployeeID;
            lblEmployeeName.Text = e.EmployeeName;
            lblGoogleAccount.Text = e.GoogleAccount;
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedEmployee == null)
            {
                MessageBox.Show("Employee data is not loaded.", "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.NavigationService.Navigate(new EditEmployeePage(LoadedEmployee, currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            EmployeePage.NotifyDataChanged();
            NavigateToEmployeeList();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedEmployee == null) return;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this employee?\nEmployee ID: {LoadedEmployee.EmployeeID}",
                "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = dataManager.DeleteEmployee(LoadedEmployee.EmployeeID);
                bool success = result != 0;
                if (!success)
                {
                    var stillThere = dataManager.GetEmployeeByID(LoadedEmployee.EmployeeID);
                    success = (stillThere == null);
                }

                if (success)
                {
                    MessageBox.Show("Employee deleted successfully!", "SUCCESS",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Minta list reload dan kembali ke list baru agar pasti ter-load
                    EmployeePage.NotifyDataChanged();
                    NavigateToEmployeeList();
                }
                else
                {
                    MessageBox.Show("Failed to delete employee.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NavigateToEmployeeList()
        {
            if (NavigationService != null)
            {
                var nav = NavigationService;
                nav.Navigated += RemoveThisPageFromBackStackOnce;
                nav.Navigate(new EmployeePage(currentUserProfile));
            }
        }

        private void RemoveThisPageFromBackStackOnce(object sender, NavigationEventArgs e)
        {
            if (sender is NavigationService nav)
            {
                nav.Navigated -= RemoveThisPageFromBackStackOnce;
                try { nav.RemoveBackEntry(); } catch { /* ignore */ }
            }
        }

        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = (profile.Names != null && profile.Names.Count > 0)
                ? profile.Names[0].DisplayName
                : "Pengguna Tidak Dikenal";

            if (profile.Photos != null && profile.Photos.Count > 0)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(profile.Photos[0].Url, UriKind.Absolute);
                    bitmap.EndInit();
                    imgUserProfile.Source = bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Profile photo load error: {ex.Message}");
                }
            }
        }
    }
}