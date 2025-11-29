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

            // 1. Validasi Input
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
                // 2. Kunci UI agar tidak diklik berkali-kali
                isSaving = true;
                btnSave.IsEnabled = false;

                // Opsional: Ubah kursor jadi loading
                this.Cursor = System.Windows.Input.Cursors.Wait;

                var newEmployee = new Employee
                {
                    // Generate ID Client Side
                    EmployeeID = "EMP-" + DateTime.UtcNow.ToString("yyMMddHHmmss"),
                    EmployeeName = txtEmployeeName.Text.Trim(),
                    GoogleAccount = txtGoogleAccount.Text.Trim()
                };

                // 3. PANGGIL METHOD ASYNC (Gunakan await)
                int result = await dataManager.InsertEmployeeAsync(newEmployee);

                bool success = result != 0;

                // Double check jika database return 0 tapi data masuk (kasus jarang)
                if (!success)
                {
                    var fetched = await dataManager.GetEmployeeByIDAsync(newEmployee.EmployeeID);
                    success = fetched != null;
                }

                if (success)
                {
                    MessageBox.Show($"Employee added successfully!",
                                    "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 4. NAVIGASI YANG BENAR:
                    // Beritahu EmployeePage bahwa ada data baru
                    EmployeePage.NotifyDataChanged();

                    // Kembali ke halaman sebelumnya (List Employee) daripada buat halaman baru
                    if (NavigationService?.CanGoBack == true)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        // Fallback jika tidak bisa back (misal langsung buka halaman ini)
                        NavigationService?.Navigate(new EmployeePage(currentUserProfile));
                    }
                }
                else
                {
                    MessageBox.Show("Failed to add employee.\nCheck database connection.",
                                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred:\n{ex.Message}",
                                "EXCEPTION", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 5. Buka kembali kunci UI
                isSaving = false;
                btnSave.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
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
                    // Silent fail for photo is better UX than popup warning
                    Console.WriteLine($"Gagal memuat foto profil: {ex.Message}");
                }
            }
        }
    }
}