using FishCycleApp.Views.Pages.Stock;
using FishCycleApp.Views.Pages.Transaction;
using Google.Apis.PeopleService.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private Person currentUserProfile;

        public DashboardWindow(Person userProfile)
        {
            InitializeComponent();
            this.currentUserProfile = userProfile;
            DashboardPage startPage = new DashboardPage(this.currentUserProfile);
            MainFrame.Navigate(startPage);
            HighlightActiveButton(btnDashboard);
        }

        public void HighlightActiveButton(Button activeButton)
        {
            Button[] menuButtons = { btnDashboard, btnStock, btnTransaction, btnClient, btnEmployee, btnSupplier };

            foreach (Button btn in menuButtons)
            {
                btn.Style = (Style)FindResource("InactiveMenuItemStyle");
                btn.IsEnabled = true;
            }

            if (activeButton != null)
            {
                activeButton.Style = (Style)FindResource("ActiveMenuItemStyle");
                activeButton.IsEnabled = false;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            HighlightActiveButton(clickedButton);

            if (clickedButton == btnStock)
            {
                MainFrame.Navigate(new StockPage(this.currentUserProfile));
            }
            else if (clickedButton == btnTransaction)
            {
                MainFrame.Navigate(new TransactionPage(this.currentUserProfile));
            }
            else if (clickedButton == btnClient)
            {
                MainFrame.Navigate(new ClientPage(this.currentUserProfile));
            }
            else if (clickedButton == btnEmployee)
            {
                MainFrame.Navigate(new EmployeePage(this.currentUserProfile));
            }
            else if (clickedButton == btnDashboard)
            {
                MainFrame.Navigate(new DashboardPage(this.currentUserProfile));
            }
            else if (clickedButton == btnSupplier)
            {
                MainFrame.Navigate(new SupplierPage(this.currentUserProfile));
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}