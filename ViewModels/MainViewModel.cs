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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StockTicker
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private CoinMarketCapService _coinMarketCapService;
        private DisplayMode _currentMode = DisplayMode.All;
        private bool _lastCallWasError = false;

        public ObservableCollection<CryptoData> AllCryptos { get; set; }
        public ObservableCollection<CryptoData> DisplayedCryptos { get; set; }
        public Configuration Configuration { get; private set; }

        public event EventHandler DataUpdated;

        public MainViewModel()
        {
            AllCryptos = new ObservableCollection<CryptoData>();
            DisplayedCryptos = new ObservableCollection<CryptoData>();
            
            // Load configuration first
            LoadConfiguration();
            _coinMarketCapService = new CoinMarketCapService(Configuration.CoinMarketCapApiKey);
            
            // Set up timer with loaded configuration
            SetupTimer();
            
            // Initial data load
            _ = LoadCryptoDataAsync();
        }

        private void SetupTimer()
        {
            // Stop existing timer if it exists
            _timer?.Stop();
            
            // Create new timer with configured interval
            var intervalMinutes = Math.Max(1, Configuration.RefreshIntervalMinutes); // Ensure minimum 1 minute
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };
            _timer.Tick += async (s, e) => await LoadCryptoDataAsync();
            _timer.Start();
            
            System.Diagnostics.Debug.WriteLine($"Timer set to refresh every {intervalMinutes} minutes");
        }

        public void LoadConfiguration()
        {
            var oldApiKey = Configuration?.CoinMarketCapApiKey ?? "";
            var oldCryptos = Configuration?.CryptoCurrencies?.ToList() ?? new List<string>();
            var oldRefreshInterval = Configuration?.RefreshIntervalMinutes ?? 0;
            
            // Load configuration from file
            Configuration = ConfigurationService.LoadConfiguration();
            _currentMode = Configuration.LastDisplayMode;

            // Ensure refresh interval has a valid value
            if (Configuration.RefreshIntervalMinutes < 1 || Configuration.RefreshIntervalMinutes > 1440)
            {
                Configuration.RefreshIntervalMinutes = 5; // Default to 5 minutes if invalid
                System.Diagnostics.Debug.WriteLine("Invalid refresh interval found, defaulting to 5 minutes");
                ConfigurationService.SaveConfiguration(Configuration); // Save corrected configuration
            }

            // Check if we need to recreate the service or refresh data
            bool shouldRefreshData = false;
            bool shouldUpdateTimer = false;

            // Recreate service if API key changed
            if (oldApiKey != Configuration.CoinMarketCapApiKey)
            {
                _coinMarketCapService?.Dispose();
                _coinMarketCapService = new CoinMarketCapService(Configuration.CoinMarketCapApiKey);
                shouldRefreshData = true;
                System.Diagnostics.Debug.WriteLine($"API key changed, recreating service");
            }

            // Check if cryptocurrency list changed
            if (!oldCryptos.SequenceEqual(Configuration.CryptoCurrencies))
            {
                shouldRefreshData = true;
                System.Diagnostics.Debug.WriteLine("Cryptocurrency list changed");
            }

            // Check if refresh interval changed (only if we had a previous configuration)
            if (oldRefreshInterval > 0 && oldRefreshInterval != Configuration.RefreshIntervalMinutes)
            {
                shouldUpdateTimer = true;
                System.Diagnostics.Debug.WriteLine($"Refresh interval changed from {oldRefreshInterval} to {Configuration.RefreshIntervalMinutes} minutes");
            }

            // Update timer if interval changed (but not on initial load)
            if (shouldUpdateTimer && _timer != null)
            {
                SetupTimer();
            }

            // Only refresh if configuration actually changed
            if (shouldRefreshData)
            {
                _ = LoadCryptoDataAsync();
            }
        }

        public void SaveConfiguration()
        {
            Configuration.LastDisplayMode = _currentMode;
            ConfigurationService.SaveConfiguration(Configuration);
            System.Diagnostics.Debug.WriteLine("Configuration saved");
        }

        public async Task RefreshDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("Manual refresh requested");
            await LoadCryptoDataAsync();
        }

        private async Task LoadCryptoDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading crypto data for {Configuration.CryptoCurrencies.Count} symbols");
                var cryptoData = await _coinMarketCapService.GetCryptocurrencyDataAsync(Configuration.CryptoCurrencies);
                
                // Check if this is error data
                bool isErrorData = cryptoData.Any(c => c.IsErrorState);
                
                // Update the AllCryptos collection on the UI thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    AllCryptos.Clear();
                    foreach (var crypto in cryptoData)
                    {
                        AllCryptos.Add(crypto);
                    }
                    UpdateDisplayedCryptos();
                    DataUpdated?.Invoke(this, EventArgs.Empty);
                    
                    // Log error state change
                    if (isErrorData && !_lastCallWasError)
                    {
                        System.Diagnostics.Debug.WriteLine("API Error: Displaying error indicators");
                    }
                    else if (!isErrorData && _lastCallWasError)
                    {
                        System.Diagnostics.Debug.WriteLine("API Recovered: Displaying real data");
                    }
                    
                    _lastCallWasError = isErrorData;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading crypto data: {ex.Message}");
                
                // Create error data manually if service throws exception
                App.Current.Dispatcher.Invoke(() =>
                {
                    AllCryptos.Clear();
                    foreach (var symbol in Configuration.CryptoCurrencies)
                    {
                        AllCryptos.Add(new CryptoData
                        {
                            Symbol = symbol,
                            Name = symbol,
                            Price = 0,
                            Change = 0,
                            ChangePercent = 0,
                            IsErrorState = true
                        });
                    }
                    UpdateDisplayedCryptos();
                    DataUpdated?.Invoke(this, EventArgs.Empty);
                    
                    if (!_lastCallWasError)
                    {
                        System.Diagnostics.Debug.WriteLine("Service Exception: Displaying error indicators");
                    }
                    _lastCallWasError = true;
                });
            }
        }

        public void ShowAll()
        {
            _currentMode = DisplayMode.All;
            UpdateDisplayedCryptos();
            SaveConfiguration();
        }

        private void UpdateDisplayedCryptos()
        {
            DisplayedCryptos.Clear();
            
            // Show all configured cryptocurrencies
            foreach (var crypto in AllCryptos)
            {
                DisplayedCryptos.Add(crypto);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum DisplayMode
    {
        All
    }
}
