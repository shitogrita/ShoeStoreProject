using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShoeStore
{
    public partial class AddProductWindow : Window
    {
        private Product? editingProduct;

        // Путь к новому изображению, которое пользователь выбрал.
        private string? selectedImageSourcePath;

        public AddProductWindow()
        {
            InitializeComponent();

            LoadCategories();
            LoadManufacturers();
            LoadSuppliers();

            ProductIdTextBox.Text = GetNextProductId();
            ProductIdPanel.Visibility = Visibility.Collapsed;

            StockQuantityTextBox.Text = "0";
            DiscountTextBox.Text = "0";
        }

        public AddProductWindow(Product product)
        {
            InitializeComponent();

            editingProduct = product;

            LoadCategories();
            LoadManufacturers();
            LoadSuppliers();

            Title = "Редактирование товара";
            TitleTextBlock.Text = "Редактирование товара";

            ProductIdTextBox.Text = product.IdProduct;
            ProductNameTextBox.Text = product.ProductName;
            DescriptionTextBox.Text = product.Description;
            PriceTextBox.Text = product.Price.ToString();

            CategoryComboBox.SelectedValue = product.IdCategory;
            ManufacturerComboBox.SelectedValue = product.IdManufacturer;
            SupplierComboBox.SelectedValue = product.IdSupplier;

            UnitTextBox.Text = product.Unit;
            StockQuantityTextBox.Text = product.StockQuantity.ToString();
            DiscountTextBox.Text = product.Discount.ToString();

            ImagePathTextBox.Text = product.ImagePath ?? string.Empty;

            ProductIdTextBox.IsReadOnly = true;
        }

        private string GetNextProductId()
        {
            using SqlConnection connection = new SqlConnection(Database.ConnectionString);

            connection.Open();

            string query = @"
                SELECT ISNULL(
                    MAX(
                        TRY_CONVERT(
                            INT,
                            SUBSTRING(IdProduct, 2, LEN(IdProduct))
                        )
                    ),
                    100
                )
                FROM Product;
            ";

            using SqlCommand command = new SqlCommand(query, connection);

            int lastId = Convert.ToInt32(command.ExecuteScalar());

            return $"P{lastId + 1}";
        }

        private void LoadCategories()
        {
            try
            {
                CategoryComboBox.Items.Clear();

                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        IdCategory,
                        CategoryName
                    FROM Category
                    ORDER BY CategoryName;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    LookupItem item = new LookupItem
                    {
                        Id = Convert.ToInt32(reader["IdCategory"]),
                        Name = reader["CategoryName"].ToString() ?? string.Empty
                    };

                    CategoryComboBox.Items.Add(item);
                }

                if (CategoryComboBox.Items.Count > 0)
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки категорий",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LoadManufacturers()
        {
            try
            {
                ManufacturerComboBox.Items.Clear();

                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        IdManufacturer,
                        ManufacturerName
                    FROM Manufacturer
                    ORDER BY ManufacturerName;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    LookupItem item = new LookupItem
                    {
                        Id = Convert.ToInt32(reader["IdManufacturer"]),
                        Name = reader["ManufacturerName"].ToString() ?? string.Empty
                    };

                    ManufacturerComboBox.Items.Add(item);
                }

                if (ManufacturerComboBox.Items.Count > 0)
                {
                    ManufacturerComboBox.SelectedIndex = 0;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки производителей",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                SupplierComboBox.Items.Clear();

                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        IdSupplier,
                        SupplierName
                    FROM Supplier
                    ORDER BY SupplierName;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    LookupItem item = new LookupItem
                    {
                        Id = Convert.ToInt32(reader["IdSupplier"]),
                        Name = reader["SupplierName"].ToString() ?? string.Empty
                    };

                    SupplierComboBox.Items.Add(item);
                }

                if (SupplierComboBox.Items.Count > 0)
                {
                    SupplierComboBox.SelectedIndex = 0;
                }
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

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                selectedImageSourcePath = dialog.FileName;

                ImagePathTextBox.Text = dialog.FileName;
            }
        }

        private string SaveImageToApplicationFolder(string sourcePath)
        {
            // Изображения приложения будут храниться в папке Images.
            string imagesDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Images"
            );

            Directory.CreateDirectory(imagesDirectory);

            string fileName =
                $"{ProductIdTextBox.Text.Trim()}_{DateTime.Now.Ticks}.jpg";

            string destinationPath =
                Path.Combine(imagesDirectory, fileName);

            BitmapImage sourceImage = new BitmapImage();

            sourceImage.BeginInit();

            // OnLoad нужен, чтобы исходный файл не оставался заблокированным.
            sourceImage.CacheOption = BitmapCacheOption.OnLoad;
            sourceImage.UriSource = new Uri(sourcePath);

            sourceImage.EndInit();

            double scaleX = 300.0 / sourceImage.PixelWidth;
            double scaleY = 200.0 / sourceImage.PixelHeight;

            double scale = Math.Min(scaleX, scaleY);

            // Маленькие изображения увеличивать не будем.
            if (scale > 1.0)
            {
                scale = 1.0;
            }

            BitmapSource finalImage = sourceImage;

            if (scale < 1.0)
            {
                finalImage = new TransformedBitmap(
                    sourceImage,
                    new ScaleTransform(scale, scale)
                );
            }

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.Frames.Add(
                BitmapFrame.Create(finalImage)
            );

            using FileStream stream =
                new FileStream(
                    destinationPath,
                    FileMode.Create
                );

            encoder.Save(stream);

            return destinationPath;
        }

        private void DeleteOldImage(string? oldImagePath, string? newImagePath)
        {
            if (string.IsNullOrWhiteSpace(oldImagePath))
            {
                return;
            }

            if (oldImagePath == newImagePath)
            {
                return;
            }

            string imagesDirectory = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Images"
                )
            );

            string oldPath =
                Path.GetFullPath(oldImagePath);

            // Удаляем только изображения из нашей папки Images.
            // Так мы случайно не удалим чужой файл пользователя.
            if (!oldPath.StartsWith(
                    imagesDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }

        private void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductIdTextBox.Text))
            {
                MessageBox.Show("Некорректный артикул.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
            {
                MessageBox.Show("Введите название.");
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Некорректная цена.");
                return;
            }

            if (!int.TryParse(StockQuantityTextBox.Text, out int stockQuantity) || stockQuantity < 0)
            {
                MessageBox.Show("Некорректное количество.");
                return;
            }

            if (!decimal.TryParse(DiscountTextBox.Text, out decimal discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка должна быть от 0 до 100.");
                return;
            }

            if (CategoryComboBox.SelectedValue == null ||
                ManufacturerComboBox.SelectedValue == null ||
                SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show(
                    "Выберите категорию, производителя и поставщика."
                );

                return;
            }

            int categoryId =
                Convert.ToInt32(CategoryComboBox.SelectedValue);

            int manufacturerId =
                Convert.ToInt32(ManufacturerComboBox.SelectedValue);

            int supplierId =
                Convert.ToInt32(SupplierComboBox.SelectedValue);

            string? imagePath =
                editingProduct?.ImagePath;

            // Если пользователь выбрал новое изображение,
            // копируем его в папку приложения.
            if (!string.IsNullOrWhiteSpace(selectedImageSourcePath))
            {
                imagePath =
                    SaveImageToApplicationFolder(
                        selectedImageSourcePath
                    );
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(
                        Database.ConnectionString
                    );

                connection.Open();

                if (editingProduct == null)
                {
                    string query = @"
                        INSERT INTO Product
                        (
                            IdProduct,
                            ProductName,
                            Price,
                            IdCategory,
                            IdSupplier,
                            Description,
                            IdManufacturer,
                            Unit,
                            StockQuantity,
                            Discount,
                            ImagePath
                        )
                        VALUES
                        (
                            @IdProduct,
                            @ProductName,
                            @Price,
                            @IdCategory,
                            @IdSupplier,
                            @Description,
                            @IdManufacturer,
                            @Unit,
                            @StockQuantity,
                            @Discount,
                            @ImagePath
                        );
                    ";

                    using SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        );

                    AddParameters(
                        command,
                        categoryId,
                        manufacturerId,
                        supplierId,
                        price,
                        stockQuantity,
                        discount,
                        imagePath
                    );

                    command.ExecuteNonQuery();

                    MessageBox.Show(
                        "Товар добавлен.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    string query = @"
                        UPDATE Product
                        SET
                            ProductName = @ProductName,
                            Price = @Price,
                            IdCategory = @IdCategory,
                            IdSupplier = @IdSupplier,
                            Description = @Description,
                            IdManufacturer = @IdManufacturer,
                            Unit = @Unit,
                            StockQuantity = @StockQuantity,
                            Discount = @Discount,
                            ImagePath = @ImagePath
                        WHERE IdProduct = @IdProduct;
                    ";

                    using SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        );

                    AddParameters(
                        command,
                        categoryId,
                        manufacturerId,
                        supplierId,
                        price,
                        stockQuantity,
                        discount,
                        imagePath
                    );

                    command.ExecuteNonQuery();

                    // Если была выбрана новая фотография,
                    // после успешного UPDATE удаляем старую.
                    if (!string.IsNullOrWhiteSpace(selectedImageSourcePath))
                    {
                        DeleteOldImage(
                            editingProduct.ImagePath,
                            imagePath
                        );
                    }

                    MessageBox.Show(
                        "Товар изменён.",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                DialogResult = true;

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка сохранения товара",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

        }

        private void AddParameters(SqlCommand command, int categoryId, int manufacturerId, int supplierId, decimal price, int stockQuantity, decimal discount, string? imagePath)
        {
            command.Parameters.AddWithValue(
                "@IdProduct",
                ProductIdTextBox.Text.Trim()
            );

            command.Parameters.AddWithValue(
                "@ProductName",
                ProductNameTextBox.Text.Trim()
            );

            command.Parameters.AddWithValue(
                "@Price",
                price
            );

            command.Parameters.AddWithValue(
                "@IdCategory",
                categoryId
            );

            command.Parameters.AddWithValue(
                "@IdSupplier",
                supplierId
            );

            command.Parameters.AddWithValue(
                "@Description",
                DescriptionTextBox.Text.Trim()
            );

            command.Parameters.AddWithValue(
                "@IdManufacturer",
                manufacturerId
            );

            command.Parameters.AddWithValue(
                "@Unit",
                UnitTextBox.Text.Trim()
            );

            command.Parameters.AddWithValue(
                "@StockQuantity",
                stockQuantity
            );

            command.Parameters.AddWithValue(
                "@Discount",
                discount
            );

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                command.Parameters.AddWithValue(
                    "@ImagePath",
                    DBNull.Value
                );
            }
            else
            {
                command.Parameters.AddWithValue(
                    "@ImagePath",
                    imagePath
                );
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}