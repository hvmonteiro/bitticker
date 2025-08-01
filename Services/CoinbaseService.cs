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
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BitTicker
{
    public class CoinbaseService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.exchange.coinbase.com/";

        public string ExchangeName => ExchangeInfo.Coinbase;
        public bool RequiresApiKey => false; // Public endpoints available

        public CoinbaseService(string apiKey = "", ILoggingService? loggingService = null)
        {
            _apiKey = apiKey;
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BitTicker/1.0");
        }

        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            try
            {
                _loggingService?.LogInfo(ExchangeName, $"Starting data fetch for {symbols.Count} symbols");
                var cryptoList = new List<CryptoData>();

                // Get 24hr stats for each symbol
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var coinbaseSymbol = symbol.ToUpper() + "-USD";
                        var statsUrl = $"{BaseUrl}products/{coinbaseSymbol}/stats";
                        var tickerUrl = $"{BaseUrl}products/{coinbaseSymbol}/ticker";

                        _loggingService?.LogHttpRequest(ExchangeName, "GET", statsUrl);
                        var statsResponse = await _httpClient.GetStringAsync(statsUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{statsResponse.Length} bytes");

                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        var tickerResponse = await _httpClient.GetStringAsync(tickerUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{tickerResponse.Length} bytes");
                        
                        var stats = JsonConvert.DeserializeObject<CoinbaseStats>(statsResponse);
                        var ticker = JsonConvert.DeserializeObject<CoinbaseTicker>(tickerResponse);

                        if (stats != null && ticker != null && 
                            !string.IsNullOrEmpty(ticker.Price) &&
                            !string.IsNullOrEmpty(stats.Open) &&
                            decimal.TryParse(ticker.Price, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                            decimal.TryParse(stats.Open, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal openPrice) &&
                            price > 0 && openPrice > 0)
                        {
                            var change = price - openPrice;
                            var changePercent = (change / openPrice) * 100;
                            var volume24h = decimal.TryParse(stats.Volume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal volume) ? volume : 0;

                            cryptoList.Add(new CryptoData
                            {
                                Symbol = symbol,
                                Name = GetCryptoName(symbol),
                                Price = price,
                                Change = change,
                                ChangePercent = changePercent,
                                MarketCap = 0,
                                Volume24h = volume24h,
                                OpenPrice = openPrice,
                                HighPrice = 0, // Not available in Coinbase API
                                LowPrice = 0, // Not available in Coinbase API
                                BidPrice = 0, // Not available in Coinbase API  
                                AskPrice = 0, // Not available in Coinbase API
                                QuoteVolume = 0, // Not available in Coinbase API
                                LastUpdateTime = DateTime.Now,
                                ExchangeName = ExchangeName,
                                IsErrorState = false,
                                IsNoDataState = false
                            });
                            
                            _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${price:F2} ({changePercent:F2}%) - Data parsed successfully");
                        }
                        else
                        {
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid data for {symbol}");
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _loggingService?.LogHttpError(ExchangeName, $"products/{symbol.ToUpper()}-USD", httpEx.Message);
                        
                        if (httpEx.Message.Contains("404"))
                        {
                            cryptoList.Add(GetNoDataForSymbol(symbol)); // Symbol not found
                        }
                        else
                        {
                            cryptoList.Add(GetErrorDataForSymbol(symbol)); // API error
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError(ExchangeName, $"Error processing {symbol}: {ex.Message}");
                        cryptoList.Add(GetErrorDataForSymbol(symbol));
                    }
                }

                var successCount = cryptoList.Count(c => !c.IsErrorState && !c.IsNoDataState);
                var errorCount = cryptoList.Count(c => c.IsErrorState);
                var noDataCount = cryptoList.Count(c => c.IsNoDataState);
                
                _loggingService?.LogInfo(ExchangeName, $"Data fetch completed: {successCount} successful, {errorCount} errors, {noDataCount} no data");
                return cryptoList;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(ExchangeName, $"General API error: {ex.Message}");
                return GetErrorData(symbols);
            }
        }

        private CryptoData GetErrorDataForSymbol(string symbol)
        {
            return new CryptoData
            {
                Symbol = symbol,
                Name = GetCryptoName(symbol),
                Price = 0,
                Change = 0,
                ChangePercent = 0,
                ExchangeName = ExchangeName,
                LastUpdateTime = DateTime.Now,
                IsErrorState = true,  // API threw an error
                IsNoDataState = false
            };
        }

        private CryptoData GetNoDataForSymbol(string symbol)
        {
            return new CryptoData
            {
                Symbol = symbol,
                Name = GetCryptoName(symbol),
                Price = 0,
                Change = 0,
                ChangePercent = 0,
                ExchangeName = ExchangeName,
                LastUpdateTime = DateTime.Now,
                IsErrorState = false,
                IsNoDataState = true  // No data received
            };
        }

        private List<CryptoData> GetErrorData(List<string> symbols)
        {
            return symbols.Select(symbol => GetErrorDataForSymbol(symbol)).ToList();
        }

        private string GetCryptoName(string symbol)
        {
            var names = new Dictionary<string, string>
            {
                ["BTC"] = "Bitcoin",
                ["ETH"] = "Ethereum",
                ["BNB"] = "BNB",
                ["XRP"] = "XRP",
                ["SOL"] = "Solana",
                ["ADA"] = "Cardano",
                ["AVAX"] = "Avalanche",
                ["DOGE"] = "Dogecoin",
                ["TRX"] = "TRON",
                ["DOT"] = "Polkadot"
            };

            return names.ContainsKey(symbol) ? names[symbol] : symbol;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class CoinbaseStats
    {
        [JsonProperty("open")]
        public string Open { get; set; } = string.Empty;

        [JsonProperty("volume")]
        public string Volume { get; set; } = string.Empty;

        [JsonProperty("last")]
        public string Last { get; set; } = string.Empty;
    }

    public class CoinbaseTicker
    {
        [JsonProperty("price")]
        public string Price { get; set; } = string.Empty;

        [JsonProperty("size")]
        public string Size { get; set; } = string.Empty;

        [JsonProperty("time")]
        public string Time { get; set; } = string.Empty;
    }
}
