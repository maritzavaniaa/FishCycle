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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            Window newWindow = null;

            if (clickedButton == btnStock)
            {
                newWindow = new StockWindow();
            }
            else if (clickedButton == btnTransaction)
            {
                newWindow = new TransactionWindow();
            }
            else if (clickedButton == btnClient)
            {
                newWindow = new ClientWindow();
            }
            else if (clickedButton == btnEmployee)
            {
                newWindow = new EmployeeWindow();
            }

            if (newWindow != null)
            {
                newWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Dashboard sudah aktif!");
            }
        }

        private void btnDetailStock_Click(object sender, RoutedEventArgs e)
        {
            StockWindow stockWindow = new StockWindow();
            stockWindow.Show();
            this.Close();
        }

        private void btnDetailTransactions_Click(object sender, RoutedEventArgs e)
        {
            TransactionWindow transactionWindow = new TransactionWindow();
            transactionWindow.Show();
            this.Close();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}