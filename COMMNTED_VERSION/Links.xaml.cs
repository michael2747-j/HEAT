/*
    File: Links.xaml.cs
    Purpose: Provides product link data and logic for HEAT links page, including sample product entries
    Created: June 3 2025
    Last Modified: June 4 2025
    Author: Michael Melles
*/

using Microsoft.UI.Xaml.Controls; // Imports UserControl and UI controls
using System.Collections.ObjectModel; // Imports observable collection for data binding

namespace HEAT // Declares project namespace
{
    public sealed partial class Links : UserControl // Declares Links control, inherits UserControl
    {
        public ObservableCollection<ProductLink> ProductLinks { get; } = new ObservableCollection<ProductLink>(); // Collection for product links, supports UI binding

        public Links() // Constructor, sets up control and sample data
        {
            this.InitializeComponent(); // Loads XAML UI

            // Sample data
            ProductLinks.Add(new ProductLink // Adds first product sample
            {
                Name = "Example Product 1", // Product name
                Link = "https://www.example-supplier.com/product1", // Product link URL
                Price = "Price: $123.45 (real-time placeholder)", // Product price
                Image = "https://via.placeholder.com/80x80.png?text=Product+1" // Product image URL
            });
            ProductLinks.Add(new ProductLink // Adds second product sample
            {
                Name = "Example Product 2",
                Link = "https://www.example-supplier.com/product2",
                Price = "Price: $67.89 (real-time placeholder)",
                Image = "https://via.placeholder.com/80x80.png?text=Product+2"
            });
            ProductLinks.Add(new ProductLink // Adds third product sample
            {
                Name = "Example Product 3",
                Link = "https://www.example-supplier.com/product3",
                Price = "Price: $42.00 (real-time placeholder)",
                Image = "https://via.placeholder.com/80x80.png?text=Product+3"
            });

            this.DataContext = this; // Sets data context for UI binding
        }
    }

    public class ProductLink // Model for product link entry
    {
        public string Name { get; set; } // Product name
        public string Link { get; set; } // Product link URL
        public string Price { get; set; } // Product price
        public string Image { get; set; } // Product image URL
    }
}
