using Google.Apis.Auth.OAuth2;  
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
            string clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                MessageBox.Show(
                    "Google OAuth credentials not configured.\nPlease check your .env file.",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string[] scopes = {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            };

            try
            {
                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("FishCycleAppToken")
                );

                if (credential.Token.AccessToken != null)
                {
                    Person profile = await GetGoogleProfile(credential);

                    if (profile != null)
                    {
                        DashboardWindow dashboard = new DashboardWindow(profile);
                        dashboard.Show();
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Otentikasi dibatalkan.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Login Google: {ex.Message}", "FATAL ERROR");
            }
        }

        private async Task<Person> GetGoogleProfile(UserCredential credential)
        {
            var profile = new Person();

            const string url = "https://www.googleapis.com/oauth2/v2/userinfo";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", credential.Token.AccessToken);

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); 

                string json = await response.Content.ReadAsStringAsync();

                dynamic userInfo = JsonConvert.DeserializeObject(json);

                if (userInfo != null)
                {
                    profile.Names = new List<Name> { new Name() { DisplayName = userInfo.name } };
                    profile.Photos = new List<Photo> { new Photo() { Url = userInfo.picture } };
                }
            }
            return profile;
        }
    }
}