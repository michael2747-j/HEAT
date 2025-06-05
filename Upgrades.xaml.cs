/*
    File: Upgrades.xaml.cs
    Purpose: Implements quiz logic to recommend upgrades based on user answers in HEAT app
    Created: June 3 2025
    Last Modified: June 4 2025
    Author: Michael Melles
*/

using Microsoft.UI; // Imports UI color types
using Microsoft.UI.Xaml; // Imports base UI types
using Microsoft.UI.Xaml.Controls; // Imports controls like Button, ContentDialog
using Microsoft.UI.Xaml.Media; // Imports brush and color utilities
using System; // Imports core types
using System.Collections.Generic; // Imports generic collections
using System.Linq; // Imports LINQ for collection queries
using System.Threading.Tasks; // Imports async/await support
using Windows.UI; // Imports Color struct

namespace HEAT // Declares project namespace
{
    public sealed partial class Upgrades : UserControl // Declares Upgrades control, inherits UserControl
    {
        private int currentQuestionIndex = 0; // Tracks current quiz question index
        private List<QuizQuestion> questions; // Holds quiz questions list

        public Upgrades() // Constructor, initializes UI and loads questions
        {
            this.InitializeComponent(); // Loads XAML UI
            questions = LoadQuizQuestions(); // Loads quiz questions into list
        }

        private class QuizOption // Represents one quiz option with text and associated product
        {
            public string Text { get; set; } // Option display text
            public string AssociatedProduct { get; set; } // Product linked to option
        }

        private class QuizQuestion // Represents one quiz question with text and options
        {
            public string Text { get; set; } // Question text
            public List<QuizOption> Options { get; set; } // List of options for question
        }

        private List<QuizQuestion> LoadQuizQuestions() // Returns list of quiz questions with options and associated products
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion // Question 1
                {
                    Text = "What is your primary goal for upgrading?",
                    Options = new()
                    {
                        new QuizOption { Text = "Enterprise-grade security", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Family-safe internet access", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Secure on-the-go connectivity", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Compliance with regulations", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 2
                {
                    Text = "Where will this upgrade be primarily used?",
                    Options = new()
                    {
                        new QuizOption { Text = "Corporate offices", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "At home with kids", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "While traveling or remote work", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Educational or campus networks", AssociatedProduct = "DNSharmony" }
                    }
                },
                new QuizQuestion // Question 3
                {
                    Text = "What type of devices are most common on your network?",
                    Options = new()
                    {
                        new QuizOption { Text = "Laptops and mobile phones", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "IoT and smart home devices", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "School-issued tablets", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Mixture of workstations and servers", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 4
                {
                    Text = "How much control do you want over DNS/firewall policies?",
                    Options = new()
                    {
                        new QuizOption { Text = "Full, granular control", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Simple filtering and safe browsing", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "VPN-level tunneling and isolation", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Managed control for distributed users", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 5
                {
                    Text = "Is VPN support a priority?",
                    Options = new()
                    {
                        new QuizOption { Text = "Yes, I need a private tunnel", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "No, DNS filtering is enough", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Only for remote workers", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Yes, for all network egress", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 6
                {
                    Text = "What’s your biggest network concern?",
                    Options = new()
                    {
                        new QuizOption { Text = "External threats and malware", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Kids stumbling onto unsafe sites", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Data interception while traveling", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Unknown or unmanaged devices", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 7
                {
                    Text = "How often are devices used remotely?",
                    Options = new()
                    {
                        new QuizOption { Text = "Rarely, mostly on LAN", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Often, on public networks", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Only at home", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Consistently during travel", AssociatedProduct = "adam:GO" }
                    }
                },
                new QuizQuestion // Question 8
                {
                    Text = "Who will manage the network settings?",
                    Options = new()
                    {
                        new QuizOption { Text = "IT administrators", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Parents or teachers", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "End users via mobile app", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Third-party MSP", AssociatedProduct = "adam:ONE" }
                    }
                },
                new QuizQuestion // Question 9
                {
                    Text = "Do you want visibility into user behavior?",
                    Options = new()
                    {
                        new QuizOption { Text = "Yes, detailed reports", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Just basic logs", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Yes, but remotely", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Not necessary", AssociatedProduct = "DNSharmony" }
                    }
                },
                new QuizQuestion // Question 10
                {
                    Text = "How important is integration with tools (like MDM/SIEM)?",
                    Options = new()
                    {
                        new QuizOption { Text = "Critical", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Not needed", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "Nice to have", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Prefer out-of-the-box features", AssociatedProduct = "DNSharmony" }
                    }
                },
                new QuizQuestion // Question 11
                {
                    Text = "Do you want Safe Search or YouTube restrictions?",
                    Options = new()
                    {
                        new QuizOption { Text = "Yes, for children/students", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "No, we enforce policies another way", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Yes, for compliance", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Only on mobile devices", AssociatedProduct = "adam:GO" }
                    }
                },
                new QuizQuestion // Question 12
                {
                    Text = "Would you prefer a self-install or managed deployment?",
                    Options = new()
                    {
                        new QuizOption { Text = "Self-install on routers", AssociatedProduct = "DNSharmony" },
                        new QuizOption { Text = "MSP-managed deployment", AssociatedProduct = "adam:ONE" },
                        new QuizOption { Text = "Easy setup mobile app", AssociatedProduct = "adam:GO" },
                        new QuizOption { Text = "Custom deployment via CLI/API", AssociatedProduct = "adam:ONE" }
                    }
                }
            };
        }

        private async void LaunchQuiz_Click(object sender, RoutedEventArgs e) // Handles quiz launch button click, runs quiz dialog loop
        {
            currentQuestionIndex = 0; // Resets question index
            var productVotes = new Dictionary<string, int>(); // Tracks votes per product

            while (currentQuestionIndex < questions.Count) // Loops through all questions
            {
                var question = questions[currentQuestionIndex]; // Gets current question
                string selectedProduct = null; // Holds selected product for current question

                var dialog = new ContentDialog // Creates dialog for question
                {
                    Title = question.Text, // Sets question text as title
                    Background = new SolidColorBrush(Color.FromArgb(255, 28, 28, 28)), // Sets dark background
                    Foreground = new SolidColorBrush(Colors.White), // Sets white text
                    XamlRoot = this.XamlRoot, // Sets XAML root for dialog
                    CloseButtonText = "Cancel", // Adds cancel button
                    DefaultButton = ContentDialogButton.None // No default button
                };

                var panel = new StackPanel { Spacing = 10 }; // Creates vertical stack for options

                foreach (var option in question.Options) // Adds buttons for each option
                {
                    var btn = new Button
                    {
                        Content = option.Text, // Sets button text
                        Tag = option.AssociatedProduct, // Stores associated product in tag
                        Background = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)), // Orange background
                        Foreground = new SolidColorBrush(Colors.Black), // Black text
                        Padding = new Thickness(10), // Padding inside button
                        Margin = new Thickness(0, 4, 0, 4), // Vertical margin between buttons
                        HorizontalAlignment = HorizontalAlignment.Stretch // Stretches button horizontally
                    };

                    btn.Click += (s, args) => // Handles option button click
                    {
                        selectedProduct = (string)((Button)s).Tag; // Sets selected product from tag
                        dialog.Hide(); // Closes dialog
                    };

                    panel.Children.Add(btn); // Adds button to panel
                }

                dialog.Content = panel; // Sets panel as dialog content
                await dialog.ShowAsync(); // Shows dialog and waits for user input

                if (selectedProduct == null) return; // Exits if user cancels

                if (!productVotes.ContainsKey(selectedProduct)) // Adds product to votes if missing
                    productVotes[selectedProduct] = 0;
                productVotes[selectedProduct]++; // Increments vote count

                currentQuestionIndex++; // Advances to next question
            }

            string bestProduct = productVotes.OrderByDescending(p => p.Value).FirstOrDefault().Key; // Finds product with most votes
            await ShowResultDialog(bestProduct); // Shows result dialog for best product
        }

        private async Task ShowResultDialog(string productName) // Displays dialog with recommended product details
        {
            string details = productName switch // Maps product name to description
            {
                "adam:ONE" => "adam:ONE� is ideal for enterprise, managed networks, and compliance.",
                "DNSharmony" => "DNSharmony� is perfect for families, schools, and SMBs.",
                "adam:GO" => "adam:GO� is best for mobile professionals and travelers.",
                _ => "Please contact us for more information."
            };

            string message = $"Based on your answers, we recommend: {productName}\n\n{details}"; // Constructs message text

            await new ContentDialog // Creates and shows dialog with recommendation
            {
                Title = "Quiz Result",
                Content = message,
                CloseButtonText = "Close",
                Background = new SolidColorBrush(Color.FromArgb(255, 28, 28, 28)),
                Foreground = new SolidColorBrush(Colors.White),
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }
}
