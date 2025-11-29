using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading; // WAJIB ADA: Untuk fitur pembatalan
using System.Threading.Tasks;
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
        private string _currentEmployeeID;

        // TOKEN PEMBATALAN: Ini kuncinya
        private CancellationTokenSource _cts;

        public ViewEmployeePage(string employeeID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            _currentEmployeeID = employeeID?.Trim();

            DisplayProfileData(userProfile);

            this.Loaded += ViewEmployeePage_Loaded;
            this.Unloaded += ViewEmployeePage_Unloaded;
            this.IsVisibleChanged += ViewEmployeePage_IsVisibleChanged;
        }

        private void ViewEmployeePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Load data saat halaman pertama kali dimuat
            ReloadDataSafe(isSilent: false);
        }

        private void ViewEmployeePage_Unloaded(object sender, RoutedEventArgs e)
        {
            // STOP! Batalkan semua loading jika user pindah halaman
            _cts?.Cancel();
        }

        private void ViewEmployeePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Jika halaman kembali terlihat (misal habis tekan Back dari Edit)
            if ((bool)e.NewValue == true)
            {
                // Reload diam-diam (silent) agar data terupdate
                ReloadDataSafe(isSilent: true);
            }
        }

        // Fungsi aman untuk reload data dengan pembatalan
        private void ReloadDataSafe(bool isSilent)
        {
            // 1. Batalkan proses lama jika masih jalan
            _cts?.Cancel();
            // 2. Buat token baru untuk proses ini
            _cts = new CancellationTokenSource();

            // 3. Jalankan loading dengan token tersebut
            _ = LoadEmployeeDetailsAsync(_currentEmployeeID, isSilent, _cts.Token);
        }

        private async Task LoadEmployeeDetailsAsync(string employeeID, bool isSilent, CancellationToken token)
        {
            try
            {
                if (!isSilent) this.Cursor = System.Windows.Input.Cursors.Wait;

                // Gunakan Token saat memanggil database
                var result = await dataManager.GetEmployeeByIDAsync(employeeID, token);

                // CEK KRUSIAL: Apakah user sudah pindah halaman saat loading berjalan?
                if (token.IsCancellationRequested) return; // Kalau ya, BERHENTI DI SINI. Jangan lanjut.

                LoadedEmployee = result;

                if (LoadedEmployee != null)
                {
                    ApplyToUI(LoadedEmployee);
                }
                else
                {
                    // Hanya tampilkan error jika halaman INI masih aktif dilihat user
                    if (!isSilent && this.IsVisible)
                    {
                        this.Cursor = System.Windows.Input.Cursors.Arrow;
                        MessageBox.Show($"Employee with ID {employeeID} not found.", "ERROR",
                            MessageBoxButton.OK, MessageBoxImage.Error);

                        GoBackOrNavigateList();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Error ini normal terjadi saat dibatalkan, abaikan saja.
            }
            catch (Exception ex)
            {
                // Hanya tampilkan error lain jika halaman masih terlihat
                if (!isSilent && this.IsVisible)
                    MessageBox.Show($"Error loading details: {ex.Message}");
            }
            finally
            {
                if (!isSilent && this.IsVisible)
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
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
            if (LoadedEmployee == null) return;
            // Saat navigasi ke Edit, event Unloaded akan terpanggil dan membatalkan loading View
            this.NavigationService.Navigate(new EditEmployeePage(LoadedEmployee, currentUserProfile));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedEmployee == null) return;

            var confirm = MessageBox.Show($"Delete Employee {LoadedEmployee.EmployeeID}?", "CONFIRM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    btnDelete.IsEnabled = false;
                    this.Cursor = System.Windows.Input.Cursors.Wait;

                    // Kita tidak pakai token di sini karena proses delete harus tuntas
                    await dataManager.DeleteEmployeeAsync(LoadedEmployee.EmployeeID);

                    EmployeePage.NotifyDataChanged();
                    GoBackOrNavigateList();
                }
                finally
                {
                    btnDelete.IsEnabled = true;
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                }
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

        private void DisplayProfileData(Person profile)
        {
            if (profile?.Names?.Count > 0) lblUserName.Text = profile.Names[0].DisplayName;

            if (profile?.Photos?.Count > 0)
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
                catch { /* Ignore */ }
            }
        }
    }
}