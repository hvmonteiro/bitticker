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
    public class BitstampService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://www.bitstamp.net/api/v2/ticker/";

        public string ExchangeName => ExchangeInfo.Bitstamp;
        public bool RequiresApiKey => false;

        public BitstampService(string apiKey = "", ILoggingService? loggingService = null)
        {
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

                foreach (var symbol in symbols)
                {
                    try
                    {
                        var bitstampSymbol = ConvertToBitstampSymbol(symbol);
                        var tickerUrl = $"{BaseUrl}{bitstampSymbol}/";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        var response = await _httpClient.GetStringAsync(tickerUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var ticker = JsonConvert.DeserializeObject<BitstampTicker>(response);
                        
                        if (ticker != null && 
                            !string.IsNullOrEmpty(ticker.Last) && 
                            !string.IsNullOrEmpty(ticker.Open) &&
                            decimal.TryParse(ticker.Last, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                            decimal.TryParse(ticker.Open, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal openPrice) &&
                            price > 0 && openPrice > 0)
                        {
                            // Calculate change and percentage
                            var change = price - openPrice;
                            var changePercent = (change / openPrice) * 100;
                            
                            // Parse other values
                            var high = decimal.TryParse(ticker.High, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                            var low = decimal.TryParse(ticker.Low, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                            var volume = decimal.TryParse(ticker.Volume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                            var bidPrice = decimal.TryParse(ticker.Bid, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                            var askPrice = decimal.TryParse(ticker.Ask, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                            var vwap = decimal.TryParse(ticker.Vwap, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vw) ? vw : 0;
                            
                            // Parse timestamp
                            var lastUpdateTime = DateTime.Now;
                            if (long.TryParse(ticker.Timestamp, out long timestamp))
                            {
                                try
                                {
                                    lastUpdateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.ToLocalTime();
                                }
                                catch
                                {
                                    lastUpdateTime = DateTime.Now;
                                }
                            }
                            
                            cryptoList.Add(new CryptoData
                            {
                                Symbol = symbol,
                                Name = GetCryptoName(symbol),
                                Price = price,
                                Change = change,
                                ChangePercent = changePercent,
                                Volume24h = volume,
                                HighPrice = high,
                                LowPrice = low,
                                OpenPrice = openPrice,
                                BidPrice = bidPrice,
                                AskPrice = askPrice,
                                QuoteVolume = vwap * volume, // Approximate quote volume
                                MarketCap = 0, // Not available in public API
                                ExchangeName = ExchangeName,
                                LastUpdateTime = lastUpdateTime,
                                IsErrorState = false,
                                IsNoDataState = false
                            });
                            
                            _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${price:F2} ({changePercent:F2}%) - Data parsed successfully");
                        }
                        else
                        {
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid data received");
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _loggingService?.LogHttpError(ExchangeName, $"ticker/{ConvertToBitstampSymbol(symbol)}/", httpEx.Message);
                        
                        if (httpEx.Message.Contains("404") || httpEx.Message.Contains("400"))
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

        private string ConvertToBitstampSymbol(string symbol)
        {
            // Bitstamp uses lowercase format like btcusd
            return $"{symbol.ToLower()}usd";
        }

        private CryptoData GetErrorDataForSymbol(string symbol)
        {
            return new CryptoData
            {
                Symbol = symbol,
                Name = GetCryptoName(symbol),
                ExchangeName = ExchangeName,
                LastUpdateTime = DateTime.Now,
                IsErrorState = true,
                IsNoDataState = false
            };
        }

        private CryptoData GetNoDataForSymbol(string symbol)
        {
            return new CryptoData
            {
                Symbol = symbol,
                Name = GetCryptoName(symbol),
                ExchangeName = ExchangeName,
                LastUpdateTime = DateTime.Now,
                IsErrorState = false,
                IsNoDataState = true
            };
        }

        private List<CryptoData> GetErrorData(List<string> symbols) =>
            symbols.Select(GetErrorDataForSymbol).ToList();

        private string GetCryptoName(string symbol)
        {
            var names = new Dictionary<string, string>
            {
                ["BTC"] = "Bitcoin", ["ETH"] = "Ethereum", ["BNB"] = "BNB", ["XRP"] = "XRP",
                ["SOL"] = "Solana", ["ADA"] = "Cardano", ["AVAX"] = "Avalanche", ["DOGE"] = "Dogecoin",
                ["TRX"] = "TRON", ["DOT"] = "Polkadot"
            };
            return names.ContainsKey(symbol) ? names[symbol] : symbol;
        }

        public void Dispose() => _httpClient?.Dispose();
    }

    public class BitstampTicker
    {
        [JsonProperty("high")]
        public string? High { get; set; }

        [JsonProperty("last")]
        public string? Last { get; set; }

        [JsonProperty("timestamp")]
        public string? Timestamp { get; set; }

        [JsonProperty("bid")]
        public string? Bid { get; set; }

        [JsonProperty("vwap")]
        public string? Vwap { get; set; }

        [JsonProperty("volume")]
        public string? Volume { get; set; }

        [JsonProperty("low")]
        public string? Low { get; set; }

        [JsonProperty("ask")]
        public string? Ask { get; set; }

        [JsonProperty("open")]
        public string? Open { get; set; }

        [JsonProperty("open_24")]
        public string? Open24 { get; set; }

        [JsonProperty("percent_change_24")]
        public string? PercentChange24 { get; set; }
    }
}
