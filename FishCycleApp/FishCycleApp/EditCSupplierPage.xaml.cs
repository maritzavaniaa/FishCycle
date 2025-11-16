using FishCycleApp.DataAccess;
using FishCycleApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace FishCycleApp
{
    public partial class EditSupplierPage : Page
    {
        private SupplierDataManager supplierManager = new SupplierDataManager();
        private string SupplierID;

        public EditSupplierPage(string id)
        {
            InitializeComponent();
            SupplierID = id;
            LoadCategory();
            LoadSupplierData();
        }

        private void LoadCategory()
        {
            cmbSupplierType.Items.Clear();
            cmbSupplierType.Items.Add("Fresh Catch");
            cmbSupplierType.Items.Add("First-Hand");
            cmbSupplierType.Items.Add("Reprocessed Stock");
        }

        private void LoadSupplierData()
        {
            Supplier s = supplierManager.GetSupplierByID(SupplierID);

            if (s != null)
            {
                txtSupplierID.Text = s.SupplierID;
                txtSupplierName.Text = s.SupplierName;
                txtSupplierPhone.Text = s.SupplierPhone;
                txtSupplierAddress.Text = s.SupplierAddress;
                cmbSupplierType.SelectedItem = s.SupplierType;
            }
            else
            {
                MessageBox.Show("Supplier not found.");
                NavigationService?.GoBack();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Supplier s = new Supplier
            {
                SupplierID = SupplierID,
                SupplierType = cmbSupplierType.SelectedItem?.ToString() ?? "",
                SupplierName = txtSupplierName.Text,
                SupplierPhone = txtSupplierPhone.Text,
                SupplierAddress = txtSupplierAddress.Text,
            };

            int result = supplierManager.UpdateSupplier(s);

            if (result == 1)
            {
                MessageBox.Show("Supplier updated successfully!");
                NavigationService?.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to update supplier.");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Delete this supplier?", "Confirm",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                supplierManager.DeleteSupplier(SupplierID);
                NavigationService?.GoBack();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
