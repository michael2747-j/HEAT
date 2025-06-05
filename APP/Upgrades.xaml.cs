using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace HEAT
{
    public sealed partial class Upgrades : UserControl
    {
        private int currentQuestionIndex = 0;
        private List<QuizQuestion> questions;

        public Upgrades()
        {
            this.InitializeComponent();
            questions = LoadQuizQuestions();
        }

        private class QuizOption
        {
            public string Text { get; set; }
            public string AssociatedProduct { get; set; }
        }

        private class QuizQuestion
        {
            public string Text { get; set; }
            public List<QuizOption> Options { get; set; }
        }

        private List<QuizQuestion> LoadQuizQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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
                new QuizQuestion
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

        private async void LaunchQuiz_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex = 0;
            var productVotes = new Dictionary<string, int>();

            while (currentQuestionIndex < questions.Count)
            {
                var question = questions[currentQuestionIndex];
                string selectedProduct = null;

                var dialog = new ContentDialog
                {
                    Title = question.Text,
                    Background = new SolidColorBrush(Color.FromArgb(255, 28, 28, 28)),
                    Foreground = new SolidColorBrush(Colors.White),
                    XamlRoot = this.XamlRoot,
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.None
                };

                var panel = new StackPanel { Spacing = 10 };

                foreach (var option in question.Options)
                {
                    var btn = new Button
                    {
                        Content = option.Text,
                        Tag = option.AssociatedProduct,
                        Background = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)), // Orange
                        Foreground = new SolidColorBrush(Colors.Black),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 4, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    btn.Click += (s, args) =>
                    {
                        selectedProduct = (string)((Button)s).Tag;
                        dialog.Hide();
                    };

                    panel.Children.Add(btn);
                }

                dialog.Content = panel;
                await dialog.ShowAsync();

                if (selectedProduct == null) return;

                if (!productVotes.ContainsKey(selectedProduct))
                    productVotes[selectedProduct] = 0;
                productVotes[selectedProduct]++;

                currentQuestionIndex++;
            }

            string bestProduct = productVotes.OrderByDescending(p => p.Value).FirstOrDefault().Key;
            await ShowResultDialog(bestProduct);
        }

        private async Task ShowResultDialog(string productName)
        {
            string details = productName switch
            {
                "adam:ONE" => "adam:ONE® is ideal for enterprise, managed networks, and compliance.",
                "DNSharmony" => "DNSharmony® is perfect for families, schools, and SMBs.",
                "adam:GO" => "adam:GO™ is best for mobile professionals and travelers.",
                _ => "Please contact us for more information."
            };

            string message = $"Based on your answers, we recommend: {productName}\n\n{details}";

            await new ContentDialog
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
