using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class AddEmployeePage : Page
    {
        private readonly EmployeeDataManager dataManager = new EmployeeDataManager();
        private readonly Person currentUserProfile;

        public AddEmployeePage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text))
            {
                MessageBox.Show("Please enter employee name.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmployeeName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtGoogleAccount.Text))
            {
                MessageBox.Show("Please enter Google account.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGoogleAccount.Focus();
                return;
            }

            try
            {
                var newEmployee = new Employee
                {
                    EmployeeID = "EMP-" + DateTime.UtcNow.ToString("yyMMddHHmmss"),
                    EmployeeName = txtEmployeeName.Text.Trim(),
                    GoogleAccount = txtGoogleAccount.Text.Trim()
                };

                int result = dataManager.InsertEmployee(newEmployee);

                bool success = result != 0;
                if (!success)
                {
                    var fetched = dataManager.GetEmployeeByID(newEmployee.EmployeeID);
                    success = fetched != null;
                }

                if (success)
                {
                    MessageBox.Show($"Employee added successfully! (code: {result})",
                                    "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (NavigationService != null)
                    {
                        var nav = NavigationService;
                        nav.Navigated += RemoveAddPageFromBackStackOnce;
                        nav.Navigate(new EmployeePage(currentUserProfile));
                    }
                }
                else
                {
                    MessageBox.Show("Failed to add employee.\n\nPlease check:\n" +
                                    "1. Database connection\n" +
                                    "2. Stored procedure exists\n" +
                                    "3. Check Output window for details",
                                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "EXCEPTION", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAddPageFromBackStackOnce(object sender, NavigationEventArgs e)
        {
            if (sender is NavigationService nav)
            {
                nav.Navigated -= RemoveAddPageFromBackStackOnce;
                try { nav.RemoveBackEntry(); } catch { /* ignore */ }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
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
                    MessageBox.Show($"Gagal memuat foto profil: {ex.Message}",
                                    "Profile Photo Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}