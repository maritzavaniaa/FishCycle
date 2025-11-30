using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Threading.Tasks;
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
        private bool isProcessing = false; 

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

            _ = LoadEmployeeByIdAsync(employeeID);
        }

        private async Task LoadEmployeeByIdAsync(string employeeID)
        {
            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;
                txtEmployeeName.IsEnabled = false; 

                var found = await dataManager.GetEmployeeByIDAsync(employeeID?.Trim());

                if (found == null)
                {
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                    MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();

                    return;
                }

                WorkingEmployee = found;
                PopulateFieldsFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
            finally
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                txtEmployeeName.IsEnabled = true;
            }
        }

        private void PopulateFieldsFromModel()
        {
            if (WorkingEmployee == null) return;

            txtEmployeeID.IsReadOnly = true;
            txtEmployeeID.Text = WorkingEmployee.EmployeeID;
            txtEmployeeName.Text = WorkingEmployee.EmployeeName;
            txtGoogleAccount.Text = WorkingEmployee.GoogleAccount;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingEmployee == null || isProcessing) return;

            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text))
            {
                MessageBox.Show("Please enter employee name.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmployeeName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtGoogleAccount.Text))
            {
                MessageBox.Show("Please enter google account.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGoogleAccount.Focus();
                return;
            }

            try
            {
                isProcessing = true;
                btnSave.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                WorkingEmployee.EmployeeName = txtEmployeeName.Text.Trim();
                WorkingEmployee.GoogleAccount = txtGoogleAccount.Text.Trim();

                bool success = await dataManager.UpdateEmployeeAsync(WorkingEmployee);

                if (!success)
                {
                    var verify = await dataManager.GetEmployeeByIDAsync(WorkingEmployee.EmployeeID);
                    success = verify != null;
                    if (success && verify != null)
                    {
                        WorkingEmployee = verify;
                    }
                }

                if (success)
                {
                    PopulateFieldsFromModel();

                    MessageBox.Show("Employee updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    EmployeePage.NotifyDataChanged();

                    if (NavigationService?.CanGoBack == true)
                        NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show("Failed to update employee. Connection might be lost.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isProcessing = false;
                btnSave.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (WorkingEmployee == null || isProcessing) return;

            var id = WorkingEmployee.EmployeeID;
            var name = WorkingEmployee.EmployeeName;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Are you sure you want to delete this employee?\nID: {id}\nName: {name}",
                "CONFIRM DELETE", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                try
                {
                    isProcessing = true;
                    btnDelete.IsEnabled = false;

                    bool success = await dataManager.DeleteEmployeeAsync(id);

                    if (!success)
                    {
                        var stillThere = await dataManager.GetEmployeeByIDAsync(id);
                        success = (stillThere == null); 
                    }

                    if (success)
                    {
                        MessageBox.Show("Employee deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                        EmployeePage.NotifyDataChanged();

                        if (NavigationService?.CanGoBack == true)
                            NavigationService.GoBack();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete employee.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete Error: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    isProcessing = false;
                    btnDelete.IsEnabled = true;
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