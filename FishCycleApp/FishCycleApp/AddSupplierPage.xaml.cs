using System;
using System.Windows;
using System.Windows.Controls;
using FishCycleApp.DataAccess;
using FishCycleApp.Models;

namespace FishCycleApp
{
    public partial class AddSupplierPage : Page
    {
        private readonly SupplierDataManager supplierManager = new SupplierDataManager();

        public AddSupplierPage()
        {
            InitializeComponent();
            InitializeCategoryComboBox();
        }

        private void InitializeCategoryComboBox()
        {
            cmbSupplierCategory.Items.Clear();

            // Content = tampilan di UI
            // Tag = ENUM value (harus persis dengan enum PostgreSQL)

            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Fresh Catch",
                Tag = "Fresh Catch"
            });

            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "First-Hand",
                Tag = "First-Hand"
            });

            cmbSupplierCategory.Items.Add(new ComboBoxItem()
            {
                Content = "Reprocessed Stock",
                Tag = "Reprocessed Stock"
            });

            cmbSupplierCategory.SelectedIndex = 0; // optional
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("Supplier name cannot be empty.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbSupplierCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)cmbSupplierCategory.SelectedItem;
            string supplierTypeEnum = selectedItem.Tag?.ToString()
                                      ?? selectedItem.Content.ToString();

            Supplier s = new Supplier
            {
                SupplierID = "SID-" + DateTime.Now.ToString("yyMMddHHmmss"),
                SupplierName = txtSupplierName.Text,
                SupplierPhone = txtSupplierPhone.Text,
                SupplierAddress = txtSupplierAddress.Text,
                SupplierType = supplierTypeEnum  // ENUM aman
            };

            int result = supplierManager.InsertSupplier(s);

            if (result == 1)
            {
                MessageBox.Show("Supplier added successfully!",
                    "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Failed to add supplier.",
                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
