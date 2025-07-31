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

using System.Linq;
using System.Windows;
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
            // Load crypto currencies first (now the first field)
            CryptoTextBox.Text = string.Join(", ", _configuration.CryptoCurrencies);
            
            // Load API key second (now the second field)
            ApiKeyTextBox.Text = _configuration.CoinMarketCapApiKey;
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
