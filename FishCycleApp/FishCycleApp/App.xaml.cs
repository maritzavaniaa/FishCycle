using System.Configuration;
using System.Data;
using System.Windows;
using DotNetEnv;
using System.IO;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }
            else
            {
                MessageBox.Show(
                    ".env file not found. Please create .env file based on .env.example",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

}