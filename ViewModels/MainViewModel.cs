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
        private readonly DispatcherTimer _timer;
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
            
            LoadConfiguration();
            _coinMarketCapService = new CoinMarketCapService(Configuration.CoinMarketCapApiKey);
            
            // Initial load
            _ = LoadCryptoDataAsync();
            
            // Set up timer for 5-minute updates
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _timer.Tick += async (s, e) => await LoadCryptoDataAsync();
            _timer.Start();
        }

        public void LoadConfiguration()
        {
            var oldApiKey = Configuration?.CoinMarketCapApiKey ?? "";
            var oldCryptos = Configuration?.CryptoCurrencies?.ToList() ?? new List<string>();
            
            Configuration = ConfigurationService.LoadConfiguration();
            _currentMode = Configuration.LastDisplayMode;

            // Check if we need to recreate the service or refresh data
            bool shouldRefreshData = false;

            // Recreate service if API key changed
            if (oldApiKey != Configuration.CoinMarketCapApiKey)
            {
                _coinMarketCapService?.Dispose();
                _coinMarketCapService = new CoinMarketCapService(Configuration.CoinMarketCapApiKey);
                shouldRefreshData = true;
            }

            // Check if cryptocurrency list changed
            if (!oldCryptos.SequenceEqual(Configuration.CryptoCurrencies))
            {
                shouldRefreshData = true;
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
        }

        public async Task RefreshDataAsync()
        {
            await LoadCryptoDataAsync();
        }

        private async Task LoadCryptoDataAsync()
        {
            try
            {
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
