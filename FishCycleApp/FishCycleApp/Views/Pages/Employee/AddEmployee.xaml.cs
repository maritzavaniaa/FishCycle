using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;

namespace FishCycleApp
{
    public partial class AddEmployeePage : Page
    {
        private EmployeeDataManager dataManager = new EmployeeDataManager();
        private Person currentUserProfile;

        public AddEmployeePage(Person userProfile)
        {
            InitializeComponent();
            this.currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
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
                MessageBox.Show("Please enter Google account.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGoogleAccount.Focus();
                return;
            }

            // Membuat objek Employee
            Employee newEmployee = new Employee
            {
                EmployeeID = "EID-" + DateTime.Now.ToString("yyMMddHHmmss"), // Auto-generate ID
                EmployeeName = txtEmployeeName.Text.Trim(),
                GoogleAccount = txtGoogleAccount.Text.Trim()
            };

            // Menyimpan data Employee menggunakan data manager
            int result = dataManager.InsertEmployee(newEmployee);

            if (result == 1)
            {
                MessageBox.Show("Employee added successfully!",
                                "SUCCESS",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // Navigasi kembali jika bisa
                if (this.NavigationService?.CanGoBack == true)
                {
                    this.NavigationService.GoBack();
                }
            }
            else
            {
                MessageBox.Show("Failed to add employee.",
                                "ERROR",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Navigasi kembali jika tombol Cancel diklik
            if (this.NavigationService?.CanGoBack == true)
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