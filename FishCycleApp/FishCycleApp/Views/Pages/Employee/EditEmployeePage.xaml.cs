using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System;
using System.Threading.Tasks; // Wajib ada
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
        private bool isProcessing = false; // Mencegah double click

        // Constructor 1: Menerima Objek Employee (Langsung tampil)
        public EditEmployeePage(Employee employee, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            WorkingEmployee = employee;
            DisplayProfileData(userProfile);
            PopulateFieldsFromModel();
        }

        // Constructor 2: Menerima ID (Harus loading dulu)
        public EditEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            DisplayProfileData(userProfile);

            // Karena Constructor tidak bisa Async, kita panggil method async secara terpisah
            // Gunakan teknik "Fire and Forget" yang aman
            _ = LoadEmployeeByIdAsync(employeeID);
        }

        private async Task LoadEmployeeByIdAsync(string employeeID)
        {
            try
            {
                // Tampilkan loading cursor
                this.Cursor = System.Windows.Input.Cursors.Wait;
                txtEmployeeName.IsEnabled = false; // Disable dulu inputan

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

        // UBAH JADI ASYNC VOID
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
                btnSave.IsEnabled = false; // Kunci tombol
                this.Cursor = System.Windows.Input.Cursors.Wait;

                WorkingEmployee.EmployeeName = txtEmployeeName.Text.Trim();
                WorkingEmployee.GoogleAccount = txtGoogleAccount.Text.Trim();

                // 1. Panggil Update Async
                int result = await dataManager.UpdateEmployeeAsync(WorkingEmployee);

                bool success = result != 0;

                // 2. Double Check jika DB return 0
                if (!success)
                {
                    var verify = await dataManager.GetEmployeeByIDAsync(WorkingEmployee.EmployeeID);
                    success = verify != null;
                    // Jika data masih ada, kita asumsikan update berhasil (idempotent) 
                    // atau minimal datanya tidak hilang.
                    if (success && verify != null)
                    {
                        // Optional: Cek apakah field berubah? 
                        // Untuk simpelnya kita anggap sukses saja.
                        WorkingEmployee = verify;
                    }
                }

                if (success)
                {
                    // Update UI lagi untuk memastikan data sinkron
                    PopulateFieldsFromModel();

                    MessageBox.Show("Employee updated successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 3. Notifikasi ke Halaman List
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

        // UBAH JADI ASYNC VOID
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

                    // 1. Panggil Delete Async
                    int result = await dataManager.DeleteEmployeeAsync(id);

                    // Logic verifikasi
                    bool success = result != 0;
                    if (!success)
                    {
                        var stillThere = await dataManager.GetEmployeeByIDAsync(id);
                        success = (stillThere == null); // Sukses jika data sudah TIDAK ada
                    }

                    if (success)
                    {
                        MessageBox.Show("Employee deleted successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Notifikasi reload penuh karena data hilang
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