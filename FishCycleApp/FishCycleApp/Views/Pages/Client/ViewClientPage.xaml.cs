using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using Google.Apis.PeopleService.v1.Data;
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

        private void ReloadDataSafe(bool isSilent)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _ = LoadClientDetailsAsync(_currentClientID, isSilent, _cts.Token);
        }

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
                    _currentClientID = LoadedClient.ClientID;
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

        private void ApplyToUI(Client c)
        {
            lblClientID.Text = c.ClientID;
            lblClientName.Text = c.ClientName;
            lblClientContact.Text = c.ClientContact ?? "-";
            lblClientCategory.Text = c.ClientCategory ?? "-";
            lblClientAddress.Text = c.ClientAddress ?? "-";
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackOrNavigateList();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedClient == null) return;
            this.NavigationService.Navigate(new EditClientPage(LoadedClient, currentUserProfile));
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedClient == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete this client?\n\nName: {LoadedClient.ClientName}",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);


            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                btnDelete.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                await clientManager.DeleteClientAsync(LoadedClient.ClientID);

                var stillThere = await clientManager.GetClientByIDAsync(LoadedClient.ClientID);
                bool success = stillThere == null;

                if (success)
                {
                    MessageBox.Show(
                        "The client has been deleted successfully.",
                        "Delete Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

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

        private void GoBackOrNavigateList()
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new ClientPage(currentUserProfile));
        }

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
                }
            }
        }
    }
}