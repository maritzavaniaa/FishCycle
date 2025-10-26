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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FishCycleApp
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void btnDetailStock_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow parentWindow = Window.GetWindow(this) as DashboardWindow;

            if (parentWindow != null)
            {
                parentWindow.HighlightActiveButton(parentWindow.btnStock);
            }

            NavigationService.Navigate(new StockPage());
        }

        private void btnDetailTransactions_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow parentWindow = Window.GetWindow(this) as DashboardWindow;

            if (parentWindow != null) 
            {
                parentWindow.HighlightActiveButton(parentWindow.btnTransaction);
            }

            NavigationService.Navigate(new TransactionPage());
        }
    }
}