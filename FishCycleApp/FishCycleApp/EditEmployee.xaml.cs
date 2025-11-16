using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class EditEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private Person currentUserProfile;
        private string CurrentEmployeeID;

        public EditEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            this.CurrentEmployeeID = employeeID;
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadEmployeeDetails(employeeID);
            txtEmployeeID.IsReadOnly = true;
        }

        private void LoadEmployeeDetails(string employeeID)
        {
            Employee employee = dataManager.GetEmployeeByID(employeeID);
            if (employee != null)
            {
                txtEmployeeID.Text = employee.EmployeeID;
                txtEmployeeName.Text = employee.EmployeeName;
                txtGoogleAccount.Text = employee.GoogleAccount;
            }
            else
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input kosong
            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text))
            {
                MessageBox.Show("Please enter employee name.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmployeeName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtGoogleAccount.Text))
            {
                MessageBox.Show("Please enter google account.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGoogleAccount.Focus();
                return;
            }

            Employee updatedEmployee = new Employee
            {
                EmployeeID = CurrentEmployeeID,
                EmployeeName = txtEmployeeName.Text.Trim(),
                GoogleAccount = txtGoogleAccount.Text.Trim()
            };

            int result = dataManager.UpdateEmployee(updatedEmployee);

            if (result == 1)
            {
                MessageBox.Show("Employee updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                this.NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this employee?\nEmployee ID: {CurrentEmployeeID}\nEmployee Name: {txtEmployeeName.Text}",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = dataManager.DeleteEmployee(CurrentEmployeeID);

                if (result == 1)
                {
                    MessageBox.Show("Employee deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
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