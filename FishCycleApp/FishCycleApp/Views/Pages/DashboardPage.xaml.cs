using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using FishCycleApp.Views.Pages.Stock;
using FishCycleApp.Views.Pages.Transaction;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FishCycleApp
{
    public partial class DashboardPage : Page
    {
        private readonly Person _currentUserProfile;
        private readonly WeatherService weatherService = new WeatherService();
        private bool _isWeatherLoading;
        private readonly TransactionDataManager _transactionDataManager = new TransactionDataManager();
        private readonly ProductDataManager _productDataManager = new ProductDataManager();
        private readonly ClientDataManager _clientDataManager = new ClientDataManager();

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
            try
            {
                var trans = await _transactionDataManager.LoadTransactionDataAsync();
                var now = DateTime.Now;

                var monthTrans = trans.Where(t => t.TransactionDate.Month == now.Month && t.TransactionDate.Year == now.Year).ToList();

                decimal revenue = monthTrans.Where(t => t.PaymentStatus == "Paid").Sum(t => t.TotalAmount);
                int count = monthTrans.Count;
                int activeDelivery = trans.Count(t => t.DeliveryStatus == "In Transit" || t.DeliveryStatus == "Pending");

                txtTotalRevenue.Text = $"Rp {revenue:N0}";
                txtTotalTransaction.Text = count.ToString();
                txtActiveDelivery.Text = activeDelivery.ToString();
            }
            catch { }
        }

        private async Task LoadFishStockAsync()
        {
            try
            {
                var products = await _productDataManager.LoadProductDataAsync();

                // Total Quantity
                decimal totalQty = products.Sum(p => p.Quantity);
                txtTotalFishStock.Text = $"{totalQty:N0} kg";

                // Top 5 Stock
                var top5 = products.OrderByDescending(p => p.Quantity).Take(5).ToList();

                // Helper function untuk set text aman
                void SetText(TextBlock nameTxt, TextBlock qtyTxt, int index)
                {
                    if (index < top5.Count)
                    {
                        nameTxt.Text = top5[index].ProductName;
                        qtyTxt.Text = $"{top5[index].Quantity:N0} kg";
                    }
                    else
                    {
                        nameTxt.Text = "-";
                        qtyTxt.Text = "0 kg";
                    }
                }

                SetText(stockItem1Name, stockItem1Qty, 0);
                SetText(stockItem2Name, stockItem2Qty, 1);
                SetText(stockItem3Name, stockItem3Qty, 2);
                SetText(stockItem4Name, stockItem4Qty, 3);
                SetText(stockItem5Name, stockItem5Qty, 4);
            }
            catch { }
        }

        private async void LoadTodayTransactions()
        {
            try
            {
                var allTransactions = await _transactionDataManager.LoadTransactionDataAsync();
                var today = DateTime.Now.Date;
                var todayList = allTransactions
                                    .Where(t => t.TransactionDate.ToLocalTime().Date == today)
                                    .OrderByDescending(t => t.TransactionDate)
                                    .ToList();
                var clients = await _clientDataManager.LoadClientDataAsync();

                var formattedList = todayList.Select(t =>
                {
                    // Cari Nama Client berdasarkan ID
                    var clientName = clients.FirstOrDefault(c => c.ClientID == t.ClientID)?.ClientName ?? "-";

                    return new
                    {
                        TransactionNumber = t.TransactionID,
                        ClientName = clientName, // <--- Nama Client Terisi di sini
                        Amount = $"Rp {t.TotalAmount:N0}",
                        TransactionStatus = t.PaymentStatus,
                        DeliveryStatus = t.DeliveryStatus
                    };
                }).ToList();

                dgTodayTransactions.ItemsSource = formattedList;
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

            this.Loaded += DashboardPage_Loaded;   // tambahkan ini
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll(
                LoadWeatherPanelAsync(),
                LoadDashboardStatsAndTableAsync(),
                LoadFishStockAsync()
            );
        }

        private async Task LoadDashboardStatsAndTableAsync()
        {
            try
            {
                // A. Ambil SEMUA Transaksi & Client
                var transactions = await _transactionDataManager.LoadTransactionDataAsync();
                var clients = await _clientDataManager.LoadClientDataAsync();

                // --- HITUNG STATISTIK (C# Calculation) ---
                var now = DateTime.Now;
                var thisMonthTransactions = transactions
                    .Where(t => t.TransactionDate.ToLocalTime().Month == now.Month &&
                                t.TransactionDate.ToLocalTime().Year == now.Year)
                    .ToList();

                // 1. Total Revenue (Bulan Ini, Status Paid)
                decimal revenue = thisMonthTransactions
                    .Where(t => t.PaymentStatus == "Paid")
                    .Sum(t => t.TotalAmount);

                // 2. Total Transaction Count (Bulan Ini)
                int transCount = thisMonthTransactions.Count;

                // 3. Active Delivery (Hari Ini, Status Pending/In Transit)
                int activeDelivery = transactions
                    .Count(t => t.TransactionDate.ToLocalTime().Date == now.Date &&
                                (t.DeliveryStatus == "Pending" || t.DeliveryStatus == "In Transit"));

                // Update UI Statistik
                txtTotalRevenue.Text = $"Rp {revenue:N0}";
                txtTotalTransaction.Text = transCount.ToString();
                txtActiveDelivery.Text = activeDelivery.ToString();


                // --- ISI TABEL TRANSAKSI HARI INI (MANUAL JOIN) ---
                var todayList = transactions
                    .Where(t => t.TransactionDate.ToLocalTime().Date == now.Date)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                var formattedList = todayList.Select(t =>
                {
                    // MANUAL JOIN: Cari Nama Client berdasarkan ID
                    var client = clients.FirstOrDefault(c => c.ClientID == t.ClientID);
                    string clientName = client?.ClientName ?? "-"; // Jika tidak ketemu, strip

                    return new
                    {
                        TransactionNumber = t.TransactionID,
                        ClientName = clientName,        // <--- SUDAH DIPERBAIKI
                        Amount = $"Rp {t.TotalAmount:N0}",
                        TransactionStatus = t.PaymentStatus,
                        DeliveryStatus = t.DeliveryStatus
                    };
                }).ToList();

                dgTodayTransactions.ItemsSource = formattedList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard Error]: {ex.Message}");
            }
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