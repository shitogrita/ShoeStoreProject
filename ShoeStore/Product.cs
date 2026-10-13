namespace ShoeStore
{
    public class Product
    {
        public string IdProduct { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int IdCategory { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public int IdSupplier { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int IdManufacturer { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        public decimal Discount { get; set; }

        public string? ImagePath { get; set; }

        // Итоговая цена с учётом скидки.
        public decimal FinalPrice
        {
            get
            {
                return Price * (1 - Discount / 100m);
            }
        }

        // Нужно WPF, чтобы понять:
        // показывать старую зачёркнутую цену или нет.
        public bool HasDiscount
        {
            get
            {
                return Discount > 0;
            }
        }

        // По заданию скидка больше 15% выделяется зелёным.
        public bool HasHighDiscount
        {
            get
            {
                return Discount > 15;
            }
        }

        // Если картинки нет, показываем picture.png.
        public string DisplayImagePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImagePath))
                {
                    return "picture.png.jpg";
                }

                return ImagePath;
            }
        }
    }
}
