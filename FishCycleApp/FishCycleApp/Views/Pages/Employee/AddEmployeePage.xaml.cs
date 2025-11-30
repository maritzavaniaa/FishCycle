using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private bool isSaving = false; // Mencegah double click

        public AddEmployeePage(Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
        }

        // UBAH JADI ASYNC VOID
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

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
                isSaving = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                var newEmployee = new Employee
                {
                    EmployeeID = "EMP-" + DateTime.UtcNow.ToString("yyMMddHHmmss"),
                    EmployeeName = txtEmployeeName.Text.Trim(),
                    GoogleAccount = txtGoogleAccount.Text.Trim()
                };

                bool success = await dataManager.InsertEmployeeAsync(newEmployee);

                if (!success)
                {
                    var check = await dataManager.GetEmployeeByIDAsync(newEmployee.EmployeeID);
                    success = check != null;
                }

                if (success)
                {
                    MessageBox.Show("Employee added successfully!", "SUCCESS");

                    EmployeePage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                    else
                        NavigationService?.Navigate(new EmployeePage(currentUserProfile));

                    return;
                }
                else
                {
                    MessageBox.Show("Failed to add employee.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred:\n{ex.Message}", "EXCEPTION");
            }
            finally
            {
                isSaving = false;
                btnSave.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
            }
        }



        private void GoBackOrNavigateList()
        {
            if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            else NavigateToEmployeeList();
        }

        private void NavigateToEmployeeList()
        {
            NavigationService?.Navigate(new EmployeePage(currentUserProfile));
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
                    // Silent fail for photo is better UX than popup warning
                    Console.WriteLine($"Gagal memuat foto profil: {ex.Message}");
                }
            }
        }
    }
}