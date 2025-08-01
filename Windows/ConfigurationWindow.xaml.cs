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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BitTicker
{
    public partial class ConfigurationWindow : Window
    {
        private Configuration _configuration;
        private string _currentSelectedExchange = string.Empty;
        private readonly string _originalExchange = string.Empty;

        public ConfigurationWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            _originalExchange = configuration.SelectedExchangeApi ?? ExchangeInfo.CoinMarketCap;
            InitializeExchangeComboBox();
            LoadCurrentSettings();
        }

        private void InitializeExchangeComboBox()
        {
            // Clear and populate ComboBox with exchanges
            ExchangeComboBox.Items.Clear();
            foreach (var exchange in ExchangeInfo.Exchanges)
            {
                ExchangeComboBox.Items.Add(exchange);
                System.Diagnostics.Debug.WriteLine($"Added exchange to ComboBox: {exchange}");
            }
        }

        private void LoadCurrentSettings()
        {
            // Load crypto currencies (first field)
            CryptoTextBox.Text = string.Join(", ", _configuration.CryptoCurrencies ?? new List<string>());
            
            // Load selected exchange (second field)
            var selectedExchange = _configuration.SelectedExchangeApi ?? ExchangeInfo.CoinMarketCap;
            System.Diagnostics.Debug.WriteLine($"Loading configuration with selected exchange: '{selectedExchange}'");
            
            _currentSelectedExchange = selectedExchange;
            
            // Find and select the exchange in the ComboBox
            bool found = false;
            for (int i = 0; i < ExchangeComboBox.Items.Count; i++)
            {
                var itemText = ExchangeComboBox.Items[i]?.ToString();
                System.Diagnostics.Debug.WriteLine($"ComboBox item {i}: '{itemText}' (comparing with '{selectedExchange}')");
                
                if (itemText == selectedExchange)
                {
                    ExchangeComboBox.SelectedIndex = i;
                    System.Diagnostics.Debug.WriteLine($"Selected ComboBox index {i} for exchange '{selectedExchange}'");
                    found = true;
                    break;
                }
            }
            
            // If not found, select first item (CoinMarketCap)
            if (!found && ExchangeComboBox.Items.Count > 0)
            {
                ExchangeComboBox.SelectedIndex = 0;
                _currentSelectedExchange = ExchangeComboBox.Items[0]?.ToString() ?? ExchangeInfo.CoinMarketCap;
                System.Diagnostics.Debug.WriteLine($"Exchange not found, defaulting to index 0: '{_currentSelectedExchange}'");
            }
            
            // Load API key for selected exchange (third field)
            LoadApiKeyForExchange(_currentSelectedExchange);
            
            // Update label for selected exchange
            UpdateApiKeyLabel(_currentSelectedExchange);
            
            // Load refresh interval (fourth field - in button row)
            RefreshIntervalTextBox.Text = _configuration.RefreshIntervalMinutes.ToString();
        }

        private void LoadApiKeyForExchange(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                ApiKeyTextBox.Text = "";
                return;
            }

            // Get API key for selected exchange
            var apiKey = "";
            if (_configuration.ExchangeApiKeys?.ContainsKey(exchangeName) == true)
            {
                apiKey = _configuration.ExchangeApiKeys[exchangeName] ?? "";
            }

            ApiKeyTextBox.Text = apiKey;
            System.Diagnostics.Debug.WriteLine($"Loaded API key for {exchangeName}: {(!string.IsNullOrEmpty(apiKey) ? "SET" : "EMPTY")}");
        }

        private void ExchangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExchangeComboBox.SelectedItem is string selectedExchange)
            {
                System.Diagnostics.Debug.WriteLine($"=== EXCHANGE SELECTION CHANGED ===");
                System.Diagnostics.Debug.WriteLine($"From: '{_currentSelectedExchange}' -> To: '{selectedExchange}'");
                
                // Save current API key before switching
                if (!string.IsNullOrEmpty(_currentSelectedExchange))
                {
                    var currentApiKey = ApiKeyTextBox.Text.Trim();
                    _configuration.ExchangeApiKeys ??= new Dictionary<string, string>();
                        
                    if (!string.IsNullOrEmpty(currentApiKey))
                    {
                        _configuration.ExchangeApiKeys[_currentSelectedExchange] = currentApiKey;
                        System.Diagnostics.Debug.WriteLine($"Saved API key for {_currentSelectedExchange}");
                    }
                    else if (_configuration.ExchangeApiKeys.ContainsKey(_currentSelectedExchange))
                    {
                        _configuration.ExchangeApiKeys.Remove(_currentSelectedExchange);
                        System.Diagnostics.Debug.WriteLine($"Removed API key for {_currentSelectedExchange}");
                    }
                }
                
                // CRITICAL FIX: Update the configuration object immediately!
                _currentSelectedExchange = selectedExchange;
                _configuration.SelectedExchangeApi = selectedExchange; // <- This was missing!
                
                System.Diagnostics.Debug.WriteLine($"Updated configuration.SelectedExchangeApi = '{_configuration.SelectedExchangeApi}'");
                
                // Update API key label dynamically
                UpdateApiKeyLabel(selectedExchange);
                
                // Load API key for the newly selected exchange
                LoadApiKeyForExchange(selectedExchange);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ExchangeComboBox.SelectedItem is not string: {ExchangeComboBox.SelectedItem?.GetType()?.Name ?? "null"}");
            }
        }

        private void UpdateApiKeyLabel(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
                exchangeName = "Exchange";
            
            ApiKeyLabel.Text = $"{exchangeName} API Key (optional):";
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
                System.Diagnostics.Debug.WriteLine($"=== SAVE CONFIGURATION START ===");
                System.Diagnostics.Debug.WriteLine($"Configuration.SelectedExchangeApi BEFORE final save: '{_configuration.SelectedExchangeApi}'");
                System.Diagnostics.Debug.WriteLine($"_currentSelectedExchange: '{_currentSelectedExchange}'");
                
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

                // DOUBLE-CHECK: Ensure selected exchange is saved
                if (string.IsNullOrEmpty(_currentSelectedExchange))
                {
                    MessageBox.Show("Please select a crypto exchange API.", 
                                  "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetSaveButton();
                    return;
                }

                // TRIPLE-CHECK: Force the exchange assignment
                _configuration.SelectedExchangeApi = _currentSelectedExchange;
                
                System.Diagnostics.Debug.WriteLine($"FINAL ASSIGNMENT: Configuration.SelectedExchangeApi = '{_configuration.SelectedExchangeApi}'");

                // Save API key for selected exchange (third field)
                var apiKey = ApiKeyTextBox.Text.Trim();
                _configuration.ExchangeApiKeys ??= new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(apiKey))
                {
                    _configuration.ExchangeApiKeys[_currentSelectedExchange] = apiKey;
                    System.Diagnostics.Debug.WriteLine($"Saved API key for {_currentSelectedExchange}");
                }
                else if (_configuration.ExchangeApiKeys.ContainsKey(_currentSelectedExchange))
                {
                    // Remove key if user cleared it
                    _configuration.ExchangeApiKeys.Remove(_currentSelectedExchange);
                    System.Diagnostics.Debug.WriteLine($"Removed API key for {_currentSelectedExchange}");
                }

                // Save refresh interval (fourth field)
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

                // SAVE THE CONFIGURATION TO FILE
                System.Diagnostics.Debug.WriteLine($"About to save configuration with exchange: '{_configuration.SelectedExchangeApi}'");
                ConfigurationService.SaveConfiguration(_configuration);
                System.Diagnostics.Debug.WriteLine("Configuration saved to file successfully");
                
                // VERIFY what was actually saved by loading it back
                var verifyConfig = ConfigurationService.LoadConfiguration();
                System.Diagnostics.Debug.WriteLine($"VERIFICATION: File contains exchange: '{verifyConfig.SelectedExchangeApi}'");
                System.Diagnostics.Debug.WriteLine($"=== SAVE CONFIGURATION END ===");

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex}");
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
