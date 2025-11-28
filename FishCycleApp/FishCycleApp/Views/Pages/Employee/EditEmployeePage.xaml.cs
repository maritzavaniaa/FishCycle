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
        private readonly EmployeeDataManager dataManager = new EmployeeDataManager();
        private readonly Person currentUserProfile;
        private Employee WorkingEmployee; 

        public EditEmployeePage(Employee employee, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            WorkingEmployee = employee;
            DisplayProfileData(userProfile);
            PopulateFieldsFromModel();
        }

        public EditEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            var found = dataManager.GetEmployeeByID(employeeID?.Trim());
            if (found == null)
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                return;
            }
            WorkingEmployee = found;
            PopulateFieldsFromModel();
        }

        private void PopulateFieldsFromModel()
        {
            txtEmployeeID.IsReadOnly = true;
            txtEmployeeID.Text = WorkingEmployee.EmployeeID;
            txtEmployeeName.Text = WorkingEmployee.EmployeeName;
            txtGoogleAccount.Text = WorkingEmployee.GoogleAccount;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingEmployee == null) return;

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

            WorkingEmployee.EmployeeName = txtEmployeeName.Text.Trim();
            WorkingEmployee.GoogleAccount = txtGoogleAccount.Text.Trim();

            int result = dataManager.UpdateEmployee(WorkingEmployee);
            bool success = result != 0;
            if (!success)
            {
                var verify = dataManager.GetEmployeeByID(WorkingEmployee.EmployeeID);
                success = verify != null;
                if (success) WorkingEmployee = verify;
            }

            if (success)
            {
                PopulateFieldsFromModel();

                MessageBox.Show("Employee updated successfully!", "SUCCESS",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                EmployeePage.NotifyEmployeeUpdated(WorkingEmployee);
                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update employee.", "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingEmployee == null) return;

            var id = WorkingEmployee.EmployeeID;
            var name = WorkingEmployee.EmployeeName;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this employee?\nEmployee ID: {id}\nEmployee Name: {name}",
                "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                int result = dataManager.DeleteEmployee(id);
                bool success = result != 0;
                if (!success)
                {
                    var stillThere = dataManager.GetEmployeeByID(id);
                    success = (stillThere == null);
                }

                if (success)
                {
                    MessageBox.Show("Employee deleted successfully!", "SUCCESS",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    EmployeePage.NotifyDataChanged();
                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to delete employee.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
                this.NavigationService.GoBack();
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
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
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