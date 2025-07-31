/*
Copyright (c) 2025 Hugo Monteiro

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StockTicker
{
    public partial class ConfigurationWindow : Window
    {
        private Configuration _configuration;

        public ConfigurationWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load crypto currencies (first field)
            CryptoTextBox.Text = string.Join(", ", _configuration.CryptoCurrencies);
            
            // Load API key (second field)
            ApiKeyTextBox.Text = _configuration.CoinMarketCapApiKey;
            
            // Load refresh interval (third field - in button row)
            RefreshIntervalTextBox.Text = _configuration.RefreshIntervalMinutes.ToString();
        }

        private void RefreshIntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numeric input
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RefreshIntervalTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow backspace, delete, tab, escape, enter
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || 
                e.Key == Key.Escape || e.Key == Key.Enter)
            {
                return;
            }

            // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V || e.Key == Key.X))
            {
                return;
            }

            // Allow arrow keys
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                return;
            }

            // Block everything else that's not a number
            if (e.Key < Key.D0 || e.Key > Key.D9)
            {
                if (e.Key < Key.NumPad0 || e.Key > Key.NumPad9)
                {
                    e.Handled = true;
                }
            }
        }

        private void RefreshIntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Remove any non-numeric characters (additional safety)
            var numericOnly = new string(textBox.Text.Where(c => char.IsDigit(c)).ToArray());
            
            if (textBox.Text != numericOnly)
            {
                var cursorPosition = textBox.SelectionStart;
                textBox.Text = numericOnly;
                textBox.SelectionStart = Math.Min(cursorPosition, numericOnly.Length);
            }

            // Validate range (1-1440 minutes = 1 minute to 24 hours)
            if (!string.IsNullOrEmpty(numericOnly))
            {
                if (int.TryParse(numericOnly, out int value))
                {
                    if (value > 1440) // More than 24 hours
                    {
                        textBox.Text = "1440";
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                    else if (value == 0 && numericOnly.Length > 1) // Leading zeros resulting in 0
                    {
                        textBox.Text = "1";
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Temporarily disable the save button and show saving status
                SaveButton.IsEnabled = false;
                SaveButton.Content = "Saving...";
                SaveButton.Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));

                // Parse crypto currencies (first field)
                var cryptos = CryptoTextBox.Text
                    .Split(',')
                    .Select(s => s.Trim().ToUpper())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                // Validate that at least one cryptocurrency is specified
                if (cryptos.Count == 0)
                {
                    MessageBox.Show("Please specify at least one cryptocurrency symbol.", 
                                  "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetSaveButton();
                    return;
                }

                _configuration.CryptoCurrencies = cryptos;

                // Save API key (second field)
                _configuration.CoinMarketCapApiKey = ApiKeyTextBox.Text.Trim();

                // Save refresh interval (third field)
                if (string.IsNullOrWhiteSpace(RefreshIntervalTextBox.Text))
                {
                    MessageBox.Show("Please specify a refresh interval in minutes (1-1440).", 
                                  "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetSaveButton();
                    return;
                }

                if (int.TryParse(RefreshIntervalTextBox.Text, out int intervalMinutes))
                {
                    if (intervalMinutes < 1 || intervalMinutes > 1440)
                    {
                        MessageBox.Show("Refresh interval must be between 1 and 1440 minutes (24 hours).", 
                                      "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ResetSaveButton();
                        return;
                    }
                    _configuration.RefreshIntervalMinutes = intervalMinutes;
                }
                else
                {
                    MessageBox.Show("Please enter a valid number for refresh interval.", 
                                  "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetSaveButton();
                    return;
                }

                ConfigurationService.SaveConfiguration(_configuration);

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetSaveButton();
            }
        }

        private void ResetSaveButton()
        {
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Save";
            SaveButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
