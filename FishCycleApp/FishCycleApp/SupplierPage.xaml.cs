using FishCycleApp.DataAccess;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace FishCycleApp
{
    public partial class SupplierPage : Page
    {
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private DataView SupplierDataView = null;

        public SupplierPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            DataTable table = supplierManager.LoadSupplierData();
            SupplierDataView = table.DefaultView;
            dgvSuppliers.ItemsSource = SupplierDataView;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SupplierDataView == null) return;

            string keyword = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                SupplierDataView.RowFilter = "";
            }
            else
            {
                SupplierDataView.RowFilter =
                    $"supplierid LIKE '%{keyword}%' OR supplier_name LIKE '%{keyword}%'";
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddSupplierPage());
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            var row = (DataRowView)((Button)sender).DataContext;
            NavigationService.Navigate(new ViewSupplierPage(row["supplierid"].ToString()));
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = (DataRowView)((Button)sender).DataContext;
            NavigationService.Navigate(new EditSupplierPage(row["supplierid"].ToString()));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = (DataRowView)((Button)sender).DataContext;
            string id = row["supplierid"].ToString();

            if (MessageBox.Show("Delete this supplier?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                supplierManager.DeleteSupplier(id);
                LoadData();
            }
        }
    }
}
