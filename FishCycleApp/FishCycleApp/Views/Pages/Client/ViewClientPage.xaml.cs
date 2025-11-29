using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace FishCycleApp
{
    public partial class ViewClientPage : Page
    {
        private readonly ClientDataManager clientManager = new ClientDataManager();
        private readonly Person currentUserProfile;

        private Client? LoadedClient;
        private string _currentClientID;

        private CancellationTokenSource _cts;

        public ViewClientPage(string clientID, Person userProfile)
        {
            InitializeComponent();
            currentUserProfile = userProfile;
            _currentClientID = clientID?.Trim() ?? "";

            DisplayProfileData(userProfile);

            Loaded += ViewClientPage_Loaded;
            Unloaded += ViewClientPage_Unloaded;
            IsVisibleChanged += ViewClientPage_IsVisibleChanged;
        }

        // ============================================================
        // PAGE EVENTS
        // ============================================================
        private void ViewClientPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDataSafe(false);
        }

        private void ViewClientPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void ViewClientPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
                ReloadDataSafe(true);
        }

        // ============================================================
        // SAFE RELOAD (prevent double load)
        // ============================================================
        private void ReloadDataSafe(bool isSilent)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _ = LoadClientDetailsAsync(_currentClientID, isSilent, _cts.Token);
        }

        // ============================================================
        // LOAD CLIENT DATA
        // ============================================================
        private async Task LoadClientDetailsAsync(string clientID, bool isSilent, CancellationToken token)
        {
            try
            {
                if (!isSilent)
                    Cursor = System.Windows.Input.Cursors.Wait;

                var result = await clientManager.GetClientByIDAsync(clientID);

                if (token.IsCancellationRequested) return;

                LoadedClient = result;

                if (LoadedClient != null)
                {
                    _currentClientID = LoadedClient.ClientID; // update ID jika ada perubahan
                    ApplyToUI(LoadedClient);
                }
                else if (!isSilent)
                {
                    MessageBox.Show($"Client with ID {clientID} not found.", "NOT FOUND");
                    GoBackOrNavigateList();
                }
            }
            catch (OperationCanceledException)
            {
                // ignored (normal saat navigate)
            }
            catch (Exception ex)
            {
                if (!isSilent)
                    MessageBox.Show($"Error loading client: {ex.Message}", "ERROR");
            }
            finally
            {
                if (!isSilent)
                    Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        // ============================================================
        // APPLY TO UI
        // ============================================================
        private void ApplyToUI(Client c)
        {
            lblClientID.Text = c.ClientID;
            lblClientName.Text = c.ClientName;
            lblClientContact.Text = c.ClientContact ?? "-";
            lblClientCategory.Text = c.ClientCategory ?? "-";
            lblClientAddress.Text = c.ClientAddress ?? "-";
        }

        // ============================================================
        // BUTTONS
        // ============================================================
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedClient == null) return;
            // Saat navigasi ke Edit, event Unloaded akan terpanggil dan membatalkan loading View
            this.NavigationService.Navigate(new EditClientPage(LoadedClient, currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedClient == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete client '{LoadedClient.ClientName}'?",
                "CONFIRM DELETE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                btnDelete.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                // Delete command
                await clientManager.DeleteClientAsync(LoadedClient.ClientID);

                // VALID WAY — cek sudah terhapus
                var stillThere = await clientManager.GetClientByIDAsync(LoadedClient.ClientID);
                bool success = stillThere == null;

                if (success)
                {
                    MessageBox.Show("Client deleted successfully!", "SUCCESS");
                    ClientPage.NotifyDataChanged();
                    GoBackOrNavigateList();
                }
                else
                {
                    MessageBox.Show("Failed to delete client.", "ERROR");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "ERROR");
            }
            finally
            {
                btnDelete.IsEnabled = true;
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        // ============================================================
        // NAVIGATION LOGIC (mirror Supplier)
        // ============================================================
        private void GoBackOrNavigateList()
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new ClientPage(currentUserProfile));
        }

        // ============================================================
        // PROFILE DISPLAY
        // ============================================================
        private void DisplayProfileData(Person profile)
        {
            lblUserName.Text = profile?.Names?[0]?.DisplayName ?? "Unknown User";

            if (profile?.Photos?.Count > 0)
            {
                try
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(profile.Photos[0].Url, UriKind.Absolute);
                    bmp.EndInit();
                    imgUserProfile.Source = bmp;
                }
                catch
                {
                    // ignore for UX
                }
            }
        }
    }
}
