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
        public DashboardWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new DashboardPage());
            HighlightActiveButton(btnDashboard);
        }

        public void HighlightActiveButton(Button activeButton)
        {
            Button[] menuButtons = { btnDashboard, btnStock, btnTransaction, btnClient, btnEmployee };

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
                MainFrame.Navigate(new StockPage());
            }
            else if (clickedButton == btnTransaction)
            {
                MainFrame.Navigate(new TransactionPage());
            }
            else if (clickedButton == btnClient)
            {
                MainFrame.Navigate(new ClientPage());
            }
            else if (clickedButton == btnEmployee)
            {
                MainFrame.Navigate(new EmployeePage());
            }
            else if (clickedButton == btnDashboard)
            {
                MainFrame.Navigate(new DashboardPage());
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