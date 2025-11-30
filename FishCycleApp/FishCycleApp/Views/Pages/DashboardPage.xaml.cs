using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using FishCycleApp.Views.Pages.Stock;
using FishCycleApp.Views.Pages.Transaction;
using Google.Apis.PeopleService.v1.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        private readonly Person _currentUserProfile;
        private readonly WeatherService weatherService = new WeatherService();
        private bool _isWeatherLoading;
        private readonly TransactionDataManager _transactionDataManager = new TransactionDataManager();
        private readonly ProductDataManager _productDataManager = new ProductDataManager();

        private async Task LoadWeatherPanelAsync()
        {
            try
            {
                string location = Environment.GetEnvironmentVariable("WEATHER_LOCATION");

                if (string.IsNullOrWhiteSpace(location))
                {
                    WeatherStatus.Text = "Location not set";
                    return;
                }

                var weather = await weatherService.GetCurrentWeatherAsync(location);

                WeatherTemp.Text = $"{weather.current.temp_c}°C";
                WeatherCondition.Text = weather.current.condition.text;
                WeatherWind.Text = $"{weather.current.wind_kph} kph";
                WeatherHumidity.Text = $"{weather.current.humidity}%";

                WeatherLocationName.Text = weather.location.name;

                // Load icon
                WeatherIcon.Source = new BitmapImage(new Uri("https:" + weather.current.condition.icon));

                SeaSafetyText.Text = $"{GetSeaSafetyLevel(weather.current.wind_kph)}";

                int fishingIndex = GetFishingProductivityIndex(
                    weather.current.wind_kph,
                    weather.current.humidity,
                    weather.current.precip_mm
                );

                FishingIndexText.Text = $"{fishingIndex}%";
                SupplierForecastText.Text = $"{GetSupplierForecast(fishingIndex)}";
                OperationalNotesText.Text = GetOperationalNotes(fishingIndex, GetSeaSafetyLevel(weather.current.wind_kph));

                WeatherStatus.Visibility = Visibility.Collapsed;
            }
            catch
            {
                WeatherStatus.Text = "Unable to load weather";
            }
            finally
            {
                _isWeatherLoading = false;
            }
        }

        private async Task LoadDashboardStatsAsync()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            var revenue = await _transactionDataManager.GetMonthlyRevenueAsync(year, month);
            txtTotalRevenue.Text = $"Rp{revenue:N0}";

            var totalTransactions = await _transactionDataManager.GetMonthlyTransactionCountAsync(year, month);
            txtTotalTransaction.Text = totalTransactions.ToString();

            var activeDelivery = await _transactionDataManager.GetActiveDeliveryTodayAsync();
            txtActiveDelivery.Text = activeDelivery.ToString();
        }

        private async Task LoadFishStockAsync()
        {
            var totalStock = await _productDataManager.GetTotalStockQuantityAsync();
            txtTotalFishStock.Text = $"{totalStock:N0} kg";

            var top5 = await _productDataManager.GetTop5StockAsync();

            // Bind ke UI
            stockItem1Name.Text = top5.Count > 0 ? top5[0].ProductName : "-";
            stockItem1Qty.Text = top5.Count > 0 ? $"{top5[0].Quantity:N0} kg" : "0 kg";

            stockItem2Name.Text = top5.Count > 1 ? top5[1].ProductName : "-";
            stockItem2Qty.Text = top5.Count > 1 ? $"{top5[1].Quantity:N0} kg" : "0 kg";

            stockItem3Name.Text = top5.Count > 2 ? top5[2].ProductName : "-";
            stockItem3Qty.Text = top5.Count > 2 ? $"{top5[2].Quantity:N0} kg" : "0 kg";

            stockItem4Name.Text = top5.Count > 3 ? top5[3].ProductName : "-";
            stockItem4Qty.Text = top5.Count > 3 ? $"{top5[3].Quantity:N0} kg" : "0 kg";

            stockItem5Name.Text = top5.Count > 4 ? top5[4].ProductName : "-";
            stockItem5Qty.Text = top5.Count > 4 ? $"{top5[4].Quantity:N0} kg" : "0 kg";
        }

        private async void LoadTodayTransactions()
        {
            try
            {
                var todayList = await _transactionDataManager.GetTodayTransactionsAsync();

                var formatted = todayList.Select(t => new
                {
                    TransactionNumber = t.TransactionID,
                    ClientName = t.ClientName,
                    Amount = "Rp " + t.TotalAmount.ToString("N0"),
                    TransactionStatus = t.PaymentStatus,
                    DeliveryStatus = t.DeliveryStatus
                }).ToList();

                dgTodayTransactions.ItemsSource = formatted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard] Error loading today's transactions: {ex.Message}");
            }
        }


        private string GetSeaSafetyLevel(double windKph)
        {
            if (windKph < 15) return "SAFE (low waves)";
            if (windKph < 25) return "CAUTION (moderate waves)";
            return "DANGEROUS (high waves)";
        }

        private int GetFishingProductivityIndex(double windKph, double humidity, double precipMm)
        {
            int score = 100;

            if (windKph > 15) score -= 20;
            if (windKph > 25) score -= 30;
            if (precipMm > 0.5) score -= 25;
            if (humidity < 50 || humidity > 90) score -= 10;

            if (score < 5) score = 5;
            if (score > 100) score = 100;

            return score;
        }

        private string GetSupplierForecast(int fishingIndex)
        {
            if (fishingIndex >= 75) return "High incoming supply";
            if (fishingIndex >= 50) return "Normal supply";
            if (fishingIndex >= 30) return "Low to normal";

            return "Low supply expected";
        }

        private string GetOperationalNotes(int fishingIndex, string seaSafety)
        {
            if (fishingIndex >= 75)
                return "Kondisi laut baik";
            if (fishingIndex >= 50)
                return "Aktivitas nelayan normal";
            if (fishingIndex >= 30)
                return "Potensi hasil tangkapan menurun";

            return $"Risiko stok rendah. {seaSafety} – pertimbangkan sumber suplai alternatif.";
        }


        public DashboardPage(Person userProfile)
        {
            InitializeComponent();
            _currentUserProfile = userProfile;
            DisplayProfileData(userProfile);
            LoadTodayTransactions();

            Loaded += DashboardPage_Loaded;   // tambahkan ini
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadWeatherPanelAsync();
            await LoadDashboardStatsAsync();
            await LoadFishStockAsync();
        }

        private void btnDetailStock_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow parentWindow = Window.GetWindow(this) as DashboardWindow;

            if (parentWindow != null)
            {
                parentWindow.HighlightActiveButton(parentWindow.btnStock);
            }

            NavigationService.Navigate(new StockPage(_currentUserProfile));
        }

        private void btnDetailTransactions_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow parentWindow = Window.GetWindow(this) as DashboardWindow;

            if (parentWindow != null)
            {
                parentWindow.HighlightActiveButton(parentWindow.btnTransaction);
            }

            NavigationService.Navigate(new TransactionPage(_currentUserProfile));
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