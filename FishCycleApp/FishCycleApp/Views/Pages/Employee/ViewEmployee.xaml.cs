using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FishCycleApp
{
    public partial class ViewEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private Person currentUserProfile;
        private string CurrentEmployeeID;

        public ViewEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            this.CurrentEmployeeID = employeeID;
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadEmployeeDetails(employeeID);
        }

        private void LoadEmployeeDetails(string employeeID)
        {
            Employee employee = dataManager.GetEmployeeByID(employeeID);
            if (employee != null)
            {
                lblEmployeeID.Text = employee.EmployeeID;
                lblEmployeeName.Text = employee.EmployeeName;
                lblGoogleAccount.Text = employee.GoogleAccount;
            }
            else
            {
                MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                this.NavigationService.GoBack();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new EditEmployeePage(CurrentEmployeeID, this.currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this employee?\nEmployee ID: {CurrentEmployeeID}",
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