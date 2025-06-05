using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace HEAT
{
    public sealed partial class Links : UserControl
    {
        public ObservableCollection<ProductLink> ProductLinks { get; } = new ObservableCollection<ProductLink>();

        public Links()
        {
            this.InitializeComponent();

            // Sample data
            ProductLinks.Add(new ProductLink
            {
                Name = "Example Product 1",
                Link = "https://www.example-supplier.com/product1",
                Price = "Price: $123.45 (real-time placeholder)",
                Image = "https://via.placeholder.com/80x80.png?text=Product+1"
            });
            ProductLinks.Add(new ProductLink
            {
                Name = "Example Product 2",
                Link = "https://www.example-supplier.com/product2",
                Price = "Price: $67.89 (real-time placeholder)",
                Image = "https://via.placeholder.com/80x80.png?text=Product+2"
            });
            ProductLinks.Add(new ProductLink
            {
                Name = "Example Product 3",
                Link = "https://www.example-supplier.com/product3",
                Price = "Price: $42.00 (real-time placeholder)",
                Image = "https://via.placeholder.com/80x80.png?text=Product+3"
            });

            this.DataContext = this;
        }
    }

    public class ProductLink
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public string Price { get; set; }
        public string Image { get; set; }
    }
}