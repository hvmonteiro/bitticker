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
        private DispatcherTimer? _timer;
        private ICryptoExchangeService? _exchangeService;
        private DisplayMode _currentMode = DisplayMode.All;
        private bool _lastCallWasError = false;
        private readonly ILoggingService _loggingService;

        public ObservableCollection<CryptoData> AllCryptos { get; set; } = new ObservableCollection<CryptoData>();
        public ObservableCollection<CryptoData> DisplayedCryptos { get; set; } = new ObservableCollection<CryptoData>();
        public Configuration Configuration { get; private set; } = new Configuration();

        public event EventHandler? DataUpdated;

        public MainViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            // Load configuration first
            LoadConfiguration();
            
            // Create exchange service based on configuration
            CreateExchangeService();
            
            // Set up timer with loaded configuration
            SetupTimer();
            
            // Initial data load
            _ = Task.Run(async () => await LoadCryptoDataAsync());
            
            // Log startup
            _loggingService.LogInfo("System", $"BitTicker started successfully with {Configuration.SelectedExchangeApi} exchange");
        }

		private void CreateExchangeService()
		{
			_loggingService.LogInfo("System", $"=== CREATING EXCHANGE SERVICE ===");
			_loggingService.LogInfo("System", $"Target exchange: '{Configuration.SelectedExchangeApi}'");
			
			// FORCE dispose existing service
			if (_exchangeService != null)
			{
				_loggingService.LogInfo("System", $"DISPOSING existing service: {_exchangeService.ExchangeName}");
				try
				{
					_exchangeService.Dispose();
				}
				catch (Exception ex)
				{
					_loggingService.LogWarning("System", $"Error disposing service: {ex.Message}");
				}
				_exchangeService = null;
				_loggingService.LogInfo("System", "Service set to null");
			}
			
			// Get API key for selected exchange
			var apiKey = string.Empty;
			if (Configuration.ExchangeApiKeys?.ContainsKey(Configuration.SelectedExchangeApi) == true)
			{
				apiKey = Configuration.ExchangeApiKeys[Configuration.SelectedExchangeApi] ?? string.Empty;
			}
			
			_loggingService.LogInfo("System", $"Creating service for: '{Configuration.SelectedExchangeApi}' with API key: {(!string.IsNullOrEmpty(apiKey) ? "SET" : "EMPTY")}");
			
			// FORCE create new service
			try
			{
				_exchangeService = ExchangeServiceFactory.CreateService(Configuration.SelectedExchangeApi, apiKey, _loggingService);
				
				_loggingService.LogInfo("System", $"=== SERVICE CREATION COMPLETE ===");
				_loggingService.LogInfo("System", $"Requested: '{Configuration.SelectedExchangeApi}' -> Created: '{_exchangeService.ExchangeName}'");
				
				// CRITICAL VERIFICATION
				if (_exchangeService.ExchangeName != Configuration.SelectedExchangeApi)
				{
					_loggingService.LogError("System", $"❌ CRITICAL ERROR: SERVICE MISMATCH!");
					_loggingService.LogError("System", $"Expected: '{Configuration.SelectedExchangeApi}', Got: '{_exchangeService.ExchangeName}'");
					
					// Force recreate with debugging
					_loggingService.LogInfo("System", "Attempting to recreate service...");
					_exchangeService?.Dispose();
					_exchangeService = ExchangeServiceFactory.CreateService(Configuration.SelectedExchangeApi, apiKey, _loggingService);
					_loggingService.LogInfo("System", $"Retry result: '{_exchangeService.ExchangeName}'");
				}
				else
				{
					_loggingService.LogInfo("System", "✅ Service matches configuration");
				}
			}
			catch (Exception ex)
			{
				_loggingService.LogError("System", $"Error creating service: {ex.Message}");
				// Fallback to default
				_exchangeService = ExchangeServiceFactory.CreateService(ExchangeInfo.CoinMarketCap, "", _loggingService);
			}
		}

        private void SetupTimer()
        {
            // Stop existing timer if it exists
            _timer?.Stop();
            
            // Create new timer with configured interval
            var intervalMinutes = Math.Max(1, Configuration.RefreshIntervalMinutes);
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };
            _timer.Tick += async (s, e) => await LoadCryptoDataAsync();
            _timer.Start();
            
            _loggingService.LogInfo("System", $"Timer set to refresh every {intervalMinutes} minutes");
        }

		public void LoadConfiguration()
		{
			var oldApiKey = string.Empty;
			var oldExchange = string.Empty;
			var oldCryptos = new List<string>();
			var oldRefreshInterval = 0;
			
			// Store the OLD service name for comparison
			var oldServiceName = _exchangeService?.ExchangeName ?? "none";
			
			// Store the OLD configuration values
			if (Configuration != null)
			{
				oldExchange = Configuration.SelectedExchangeApi ?? string.Empty;
				oldCryptos = Configuration.CryptoCurrencies?.ToList() ?? new List<string>();
				oldRefreshInterval = Configuration.RefreshIntervalMinutes;
				
				if (Configuration.ExchangeApiKeys?.ContainsKey(oldExchange) == true)
				{
					oldApiKey = Configuration.ExchangeApiKeys[oldExchange] ?? string.Empty;
				}
			}
			
			_loggingService.LogInfo("Config", $"=== LOADING CONFIGURATION ===");
			_loggingService.LogInfo("Config", $"OLD Exchange: '{oldExchange}' (Current Service: {oldServiceName})");
			
			// Load FRESH configuration from file
			Configuration = ConfigurationService.LoadConfiguration();
			_currentMode = Configuration.LastDisplayMode;

			_loggingService.LogInfo("Config", $"NEW Exchange: '{Configuration.SelectedExchangeApi}'");

			// Ensure we have a valid exchange selected
			if (string.IsNullOrEmpty(Configuration.SelectedExchangeApi) || 
				!ExchangeInfo.Exchanges.Contains(Configuration.SelectedExchangeApi))
			{
				Configuration.SelectedExchangeApi = ExchangeInfo.CoinMarketCap;
				_loggingService.LogWarning("Config", $"Invalid exchange, defaulting to {Configuration.SelectedExchangeApi}");
				ConfigurationService.SaveConfiguration(Configuration);
			}

			// Ensure refresh interval has a valid value
			if (Configuration.RefreshIntervalMinutes < 1 || Configuration.RefreshIntervalMinutes > 1440)
			{
				Configuration.RefreshIntervalMinutes = 5;
				_loggingService.LogWarning("Config", "Invalid refresh interval, defaulting to 5 minutes");
				ConfigurationService.SaveConfiguration(Configuration);
			}

			// Get NEW API key
			var newApiKey = string.Empty;
			if (Configuration.ExchangeApiKeys?.ContainsKey(Configuration.SelectedExchangeApi) == true)
			{
				newApiKey = Configuration.ExchangeApiKeys[Configuration.SelectedExchangeApi] ?? string.Empty;
			}

			// ENHANCED CHANGE DETECTION - This is the critical fix!
			var exchangeChanged = !string.Equals(oldExchange, Configuration.SelectedExchangeApi, StringComparison.OrdinalIgnoreCase);
			var apiKeyChanged = !string.Equals(oldApiKey, newApiKey, StringComparison.Ordinal);
			var serviceNameMismatch = !string.Equals(_exchangeService?.ExchangeName, Configuration.SelectedExchangeApi, StringComparison.OrdinalIgnoreCase);
			
			// FORCE service recreation if ANY of these conditions are true
			bool shouldRecreateService = exchangeChanged || apiKeyChanged || _exchangeService == null || serviceNameMismatch;
			bool shouldRefreshData = false;
			bool shouldUpdateTimer = false;

			_loggingService.LogInfo("Config", $"=== CHANGE DETECTION ===");
			_loggingService.LogInfo("Config", $"Exchange changed: {exchangeChanged} ('{oldExchange}' vs '{Configuration.SelectedExchangeApi}')");
			_loggingService.LogInfo("Config", $"API key changed: {apiKeyChanged}");
			_loggingService.LogInfo("Config", $"Service exists: {_exchangeService != null}");
			_loggingService.LogInfo("Config", $"Service name mismatch: {serviceNameMismatch} (Service: '{_exchangeService?.ExchangeName}' vs Config: '{Configuration.SelectedExchangeApi}')");
			_loggingService.LogInfo("Config", $"SHOULD RECREATE SERVICE: {shouldRecreateService}");

			if (shouldRecreateService)
			{
				_loggingService.LogInfo("Config", $"*** FORCING EXCHANGE SERVICE RECREATION ***");
				_loggingService.LogInfo("Config", $"Reasons: ExchangeChanged={exchangeChanged}, ApiKeyChanged={apiKeyChanged}, ServiceExists={_exchangeService != null}, ServiceMismatch={serviceNameMismatch}");
				
				// FORCE dispose old service
				if (_exchangeService != null)
				{
					_loggingService.LogInfo("Config", $"DISPOSING old service: {_exchangeService.ExchangeName}");
					_exchangeService.Dispose();
					_exchangeService = null;
				}
				
				// Create new service - FORCE recreation
				_loggingService.LogInfo("Config", $"CREATING new service for: {Configuration.SelectedExchangeApi}");
				CreateExchangeService();
				shouldRefreshData = true;
				
				_loggingService.LogInfo("Config", $"NEW SERVICE CREATED: {_exchangeService?.ExchangeName ?? "ERROR"}");
				
				// VERIFY the service matches the configuration
				if (_exchangeService?.ExchangeName != Configuration.SelectedExchangeApi)
				{
					_loggingService.LogError("Config", $"❌ SERVICE MISMATCH AFTER CREATION! Expected: '{Configuration.SelectedExchangeApi}', Got: '{_exchangeService?.ExchangeName}'");
				}
				else
				{
					_loggingService.LogInfo("Config", $"✅ SERVICE MATCHES CONFIGURATION: {_exchangeService.ExchangeName}");
				}
			}

			// Check if cryptocurrency list changed
			if (!oldCryptos.SequenceEqual(Configuration.CryptoCurrencies ?? new List<string>()))
			{
				shouldRefreshData = true;
				_loggingService.LogInfo("Config", "Cryptocurrency list changed");
			}

			// Check if refresh interval changed
			if (oldRefreshInterval > 0 && oldRefreshInterval != Configuration.RefreshIntervalMinutes)
			{
				shouldUpdateTimer = true;
				_loggingService.LogInfo("Config", $"Refresh interval changed: {oldRefreshInterval} -> {Configuration.RefreshIntervalMinutes} minutes");
			}

			// Apply remaining changes
			if (shouldUpdateTimer && _timer != null)
			{
				_loggingService.LogInfo("Config", ">>> UPDATING TIMER <<<");
				SetupTimer();
			}

			if (shouldRefreshData)
			{
				_loggingService.LogInfo("Config", $">>> TRIGGERING DATA REFRESH FROM {_exchangeService?.ExchangeName ?? "ERROR"} <<<");
				_ = Task.Run(async () => await LoadCryptoDataAsync());
			}

			if (!shouldRecreateService && !shouldRefreshData && !shouldUpdateTimer)
			{
				_loggingService.LogInfo("Config", "No significant changes detected");
			}
			
			_loggingService.LogInfo("Config", $"=== CONFIGURATION LOAD COMPLETE - ACTIVE SERVICE: {_exchangeService?.ExchangeName ?? "ERROR"} ===");
		}

        public void SaveConfiguration()
        {
            Configuration.LastDisplayMode = _currentMode;
            ConfigurationService.SaveConfiguration(Configuration);
            _loggingService.LogDebug("System", "Configuration saved");
        }

        public async Task RefreshDataAsync()
        {
            _loggingService.LogInfo("System", $"Manual refresh requested from {_exchangeService?.ExchangeName ?? "Unknown"} exchange");
            await LoadCryptoDataAsync();
        }

        private async Task LoadCryptoDataAsync()
        {
            if (_exchangeService == null)
            {
                _loggingService.LogError("System", "Exchange service is null, cannot load data");
                return;
            }

            try
            {
                _loggingService.LogInfo(_exchangeService.ExchangeName, $"Loading crypto data for {Configuration.CryptoCurrencies?.Count ?? 0} symbols");
                
                var cryptoData = await _exchangeService.GetCryptocurrencyDataAsync(Configuration.CryptoCurrencies ?? new List<string>());
                
                // Check if this is error data or no data
                bool hasErrorData = cryptoData.Any(c => c.IsErrorState);
                bool hasNoData = cryptoData.Any(c => c.IsNoDataState);
                bool isProblematic = hasErrorData || hasNoData;
                
                // Update the AllCryptos collection on the UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllCryptos.Clear();
                    foreach (var crypto in cryptoData)
                    {
                        AllCryptos.Add(crypto);
                    }
                    UpdateDisplayedCryptos();
                    DataUpdated?.Invoke(this, EventArgs.Empty);
                    
                    // Log state changes
                    if (isProblematic && !_lastCallWasError)
                    {
                        if (hasErrorData && hasNoData)
                            _loggingService.LogWarning(_exchangeService.ExchangeName, "Some API errors and missing data detected");
                        else if (hasErrorData)
                            _loggingService.LogError(_exchangeService.ExchangeName, "API errors detected - displaying $ERR indicators");
                        else if (hasNoData)
                            _loggingService.LogWarning(_exchangeService.ExchangeName, "Some symbols have no data - displaying $0 indicators");
                    }
                    else if (!isProblematic && _lastCallWasError)
                    {
                        _loggingService.LogInfo(_exchangeService.ExchangeName, "All data successfully retrieved");
                    }
                    
                    _lastCallWasError = isProblematic;
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(_exchangeService.ExchangeName, $"Error loading crypto data: {ex.Message}");
                
                // Create error data manually if service throws exception
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllCryptos.Clear();
                    foreach (var symbol in Configuration.CryptoCurrencies ?? new List<string>())
                    {
                        AllCryptos.Add(new CryptoData
                        {
                            Symbol = symbol,
                            Name = symbol,
                            Price = 0,
                            Change = 0,
                            ChangePercent = 0,
                            IsErrorState = true,  // Service exception = API error
                            IsNoDataState = false,
                            ExchangeName = _exchangeService?.ExchangeName ?? "Unknown",
                            LastUpdateTime = DateTime.Now
                        });
                    }
                    UpdateDisplayedCryptos();
                    DataUpdated?.Invoke(this, EventArgs.Empty);
                    
                    if (!_lastCallWasError)
                    {
                        _loggingService.LogError("System", "Service Exception: Displaying $ERR indicators for all symbols");
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

        public event PropertyChangedEventHandler? PropertyChanged;
        
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
