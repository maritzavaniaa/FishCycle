using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Google.Apis.Auth.OAuth2;  
using Google.Apis.Util.Store;

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
            string clientId = "";
            string clientSecret = "";

            string[] scopes = { "profile", "email" };

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
                    MessageBox.Show("Login Google Berhasil!", "SUCCESS");

                    DashboardWindow dashboard = new DashboardWindow();
                    dashboard.Show();
                    this.Close(); 
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
    }
}