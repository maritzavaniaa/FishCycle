using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace FishCycleApp
{
    public partial class ViewSupplierPage : Page
    {
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private string SupplierID;

        public ViewSupplierPage(string id)
        {
            InitializeComponent();
            SupplierID = id;
            LoadData();
        }

        private void LoadData()
        {
            Supplier s = supplierManager.GetSupplierByID(SupplierID);

            if (s != null)
            {
                lblSupplierID.Text = s.SupplierID;
                lblSupplierName.Text = s.SupplierName;
                lblSupplierPhone.Text = s.SupplierPhone;
                lblSupplierAddress.Text = s.SupplierAddress;
                lblSupplierType.Text = s.SupplierType;
            }
            else
            {
                MessageBox.Show("Supplier not found!");
                NavigationService?.GoBack();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditSupplierPage(SupplierID));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Delete this supplier?", "CONFIRM",
                                          MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                supplierManager.DeleteSupplier(SupplierID);
                NavigationService?.GoBack();
            }
        }
    }
}
