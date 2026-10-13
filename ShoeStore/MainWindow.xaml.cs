using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShoeStore
{
    public partial class MainWindow : Window
    {
        // Аналог std::vector<Product>.
        // Здесь хранится полный список товаров из БД.
        private List<Product> allProducts = new List<Product>();

        private string currentRole;

        public MainWindow(string fullName, string roleName)
        {
            InitializeComponent();

            currentRole = roleName;

            UserNameTextBlock.Text = fullName;

            // Изменять товары может только администратор.
            if (roleName != "Admin")
            {
                AddProductButton.Visibility = Visibility.Collapsed;
                EditProductButton.Visibility = Visibility.Collapsed;
                DeleteProductButton.Visibility = Visibility.Collapsed;
            }

            // Клиент и гость могут только просматривать товары.
            if (roleName == "Client" || roleName == "Guest")
            {
                SearchTextBox.Visibility = Visibility.Collapsed;
                SupplierFilterComboBox.Visibility = Visibility.Collapsed;
                SortComboBox.Visibility = Visibility.Collapsed;
                OrdersButton.Visibility = Visibility.Collapsed;
            }

            LoadSuppliers();
            LoadProducts();
        }


        private void LoadProducts()
        {
            try
            {
                allProducts.Clear();

                // using автоматически освободит SQL-ресурс.
                // По смыслу похоже на RAII в C++.
                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        p.IdProduct,
                        p.ProductName,
                        p.Price,
                        p.IdCategory,
                        c.CategoryName,
                        p.IdSupplier,
                        s.SupplierName,
                        p.Description,
                        p.IdManufacturer,
                        m.ManufacturerName,
                        p.Unit,
                        p.StockQuantity,
                        p.Discount,
                        p.ImagePath
                    FROM Product p
                    INNER JOIN Category c
                        ON p.IdCategory = c.IdCategory
                    INNER JOIN Supplier s
                        ON p.IdSupplier = s.IdSupplier
                    INNER JOIN Manufacturer m
                        ON p.IdManufacturer = m.IdManufacturer;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Product product = new Product
                    {
                        IdProduct = reader["IdProduct"].ToString() ?? string.Empty,
                        ProductName = reader["ProductName"].ToString() ?? string.Empty,
                        Price = Convert.ToDecimal(reader["Price"]),

                        IdCategory = Convert.ToInt32(reader["IdCategory"]),
                        CategoryName = reader["CategoryName"].ToString() ?? string.Empty,

                        IdSupplier = Convert.ToInt32(reader["IdSupplier"]),
                        SupplierName = reader["SupplierName"].ToString() ?? string.Empty,

                        Description = reader["Description"].ToString() ?? string.Empty,

                        IdManufacturer = Convert.ToInt32(reader["IdManufacturer"]),
                        ManufacturerName = reader["ManufacturerName"].ToString() ?? string.Empty,

                        Unit = reader["Unit"].ToString() ?? string.Empty,

                        StockQuantity = Convert.ToInt32(reader["StockQuantity"]),

                        Discount = Convert.ToDecimal(reader["Discount"]),

                        // DBNull означает SQL NULL.
                        ImagePath = reader["ImagePath"] == DBNull.Value
                            ? null
                            : reader["ImagePath"].ToString()
                    };

                    allProducts.Add(product);
                }

                ApplyFilters();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки товаров",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                SupplierFilterComboBox.Items.Clear();

                SupplierFilterComboBox.Items.Add("Все поставщики");

                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT SupplierName
                    FROM Supplier
                    ORDER BY SupplierName;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string supplierName = reader["SupplierName"].ToString() ?? string.Empty;

                    SupplierFilterComboBox.Items.Add(supplierName);
                }

                SupplierFilterComboBox.SelectedIndex = 0;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки поставщиков",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<Product> result = allProducts;

            string search = SearchTextBox.Text.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Where — LINQ-фильтрация.
                // product => ... — lambda.
                result = result.Where(product =>
                    product.IdProduct.ToLower().Contains(search) ||
                    product.ProductName.ToLower().Contains(search) ||
                    product.CategoryName.ToLower().Contains(search) ||
                    product.ManufacturerName.ToLower().Contains(search) ||
                    product.SupplierName.ToLower().Contains(search) ||
                    product.Description.ToLower().Contains(search) ||
                    product.Unit.ToLower().Contains(search)
                );
            }

            if (SupplierFilterComboBox.SelectedItem != null)
            {
                string selectedSupplier = SupplierFilterComboBox.SelectedItem.ToString() ?? string.Empty;

                if (selectedSupplier != "Все поставщики")
                {
                    result = result.Where(product => product.SupplierName == selectedSupplier);
                }
            }

            if (SortComboBox.SelectedIndex == 1)
            {
                result = result.OrderBy(product => product.StockQuantity);
            }
            else if (SortComboBox.SelectedIndex == 2)
            {
                result = result.OrderByDescending(product => product.StockQuantity);
            }

            ProductsGrid.ItemsSource = null;
            ProductsGrid.Items.Clear();
            ProductsGrid.ItemsSource = result.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyFilters();
        }

        private void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyFilters();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyFilters();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProductWindow window = new AddProductWindow
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                LoadSuppliers();
                LoadProducts();
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            // SelectedItem хранится как object.
            // Через as пытаемся получить Product.
            Product? selectedProduct = ProductsGrid.SelectedItem as Product;

            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            AddProductWindow window = new AddProductWindow(selectedProduct)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                LoadSuppliers();
                LoadProducts();
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            Product? selectedProduct = ProductsGrid.SelectedItem as Product;

            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            try
            {
                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string checkQuery = @"
                    SELECT COUNT(*)
                    FROM OrderItem
                    WHERE IdProduct = @IdProduct;
                ";

                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);

                checkCommand.Parameters.AddWithValue("@IdProduct", selectedProduct.IdProduct);

                // ExecuteScalar() используется,
                // когда SQL должен вернуть одно значение.
                int orderCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (orderCount > 0)
                {
                    MessageBox.Show(
                        "Нельзя удалить товар, который находится в заказе.",
                        "Удаление невозможно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "Удалить выбранный товар?",
                    "Удаление",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                string deleteQuery = @"
                    DELETE FROM Product
                    WHERE IdProduct = @IdProduct;
                ";

                using SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);

                deleteCommand.Parameters.AddWithValue("@IdProduct", selectedProduct.IdProduct);

                deleteCommand.ExecuteNonQuery();

                LoadProducts();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Show();

            Close();
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow window = new OrdersWindow(currentRole)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void ProductsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Редактировать товары может только администратор.
            if (currentRole != "Admin")
            {
                return;
            }

            Product? selectedProduct = ProductsGrid.SelectedItem as Product;

            if (selectedProduct == null)
            {
                return;
            }

            AddProductWindow window = new AddProductWindow(selectedProduct)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                LoadProducts();
            }
        }


    }
}