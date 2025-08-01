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
    public class BinanceService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILoggingService _loggingService;
        private const string BaseUrl = "https://api.binance.com/api/v3/";

        public string ExchangeName => ExchangeInfo.Binance;
        public bool RequiresApiKey => false; // Public endpoints available

        public BinanceService(string apiKey = "", ILoggingService loggingService = null)
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

                // Get price ticker for individual symbols to avoid rate limits
                foreach (var symbol in symbols)
                {
                    try
                    {
                        // Convert symbol to Binance format (e.g., BTC -> BTCUSDT)
                        var binanceSymbol = ConvertToBinanceSymbol(symbol);
                        
                        // Get 24hr ticker for specific symbol
                        var tickerUrl = $"{BaseUrl}ticker/24hr?symbol={binanceSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        
                        var tickerResponse = await _httpClient.GetStringAsync(tickerUrl);
                        
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{tickerResponse.Length} bytes");
                        
                        var ticker = JsonConvert.DeserializeObject<BinanceTicker>(tickerResponse);

                        if (ticker != null && 
                            !string.IsNullOrEmpty(ticker.LastPrice) && 
                            !string.IsNullOrEmpty(ticker.PriceChangePercent))
                        {
                            // Parse price from API "lastPrice" field
                            if (!decimal.TryParse(ticker.LastPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal lastPrice))
                            {
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Failed to parse lastPrice '{ticker.LastPrice}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                                continue;
                            }

                            // Parse percentage from API "priceChangePercent" field  
                            if (!decimal.TryParse(ticker.PriceChangePercent, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal priceChangePercent))
                            {
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Failed to parse priceChangePercent '{ticker.PriceChangePercent}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                                continue;
                            }

                            // Validate that we have valid data
                            if (lastPrice <= 0)
                            {
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price {lastPrice}");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                                continue;
                            }

                            // Calculate absolute change from percentage and current price
                            decimal absoluteChange = 0;
                            if (decimal.TryParse(ticker.PriceChange, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal priceChange))
                            {
                                absoluteChange = priceChange;
                            }
                            else
                            {
                                absoluteChange = lastPrice * (priceChangePercent / 100);
                            }
                            
                            // Parse additional fields for tooltip
                            var volume24h = decimal.TryParse(ticker.Volume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal volume) ? volume : 0;
                            var openPrice = decimal.TryParse(ticker.OpenPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal open) ? open : 0;
                            var highPrice = decimal.TryParse(ticker.HighPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal high) ? high : 0;
                            var lowPrice = decimal.TryParse(ticker.LowPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal low) ? low : 0;
                            var bidPrice = decimal.TryParse(ticker.BidPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                            var askPrice = decimal.TryParse(ticker.AskPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                            var quoteVolume = decimal.TryParse(ticker.QuoteVolume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;

                            // Convert closeTime timestamp to DateTime
                            var lastUpdateTime = DateTime.Now;
                            try
                            {
                                lastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(ticker.CloseTime).DateTime.ToLocalTime();
                            }
                            catch (Exception ex)
                            {
                                _loggingService?.LogDebug(ExchangeName, $"{symbol}: Failed to parse closeTime {ticker.CloseTime}: {ex.Message}");
                            }
                            
                            // Create CryptoData with all fields populated
                            cryptoList.Add(new CryptoData
                            {
                                Symbol = symbol,
                                Name = GetCryptoName(symbol),
                                Price = lastPrice,                    // ← FROM API "lastPrice"
                                Change = absoluteChange,              // Calculated from percentage or direct from API
                                ChangePercent = priceChangePercent,   // ← FROM API "priceChangePercent"
                                MarketCap = 0,                       // Not available in Binance public API
                                Volume24h = volume24h,
                                OpenPrice = openPrice,
                                HighPrice = highPrice,
                                LowPrice = lowPrice,
                                BidPrice = bidPrice,
                                AskPrice = askPrice,
                                QuoteVolume = quoteVolume,
                                LastUpdateTime = lastUpdateTime,
                                ExchangeName = ExchangeName,
                                IsErrorState = false,
                                IsNoDataState = false
                            });
                            
                            _loggingService?.LogInfo(ExchangeName, 
                                $"{symbol}: Price=${lastPrice:F2}, Change%={priceChangePercent:F2}%, " +
                                $"High=${highPrice:F2}, Low=${lowPrice:F2}, Vol={volume24h:F0}");
                        }
                        else
                        {
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: Missing required fields in API response");
                            if (ticker == null)
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Failed to deserialize JSON response");
                            else
                            {
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: lastPrice='{ticker.LastPrice}', priceChangePercent='{ticker.PriceChangePercent}'");
                            }
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        var statusCode = ExtractStatusCodeFromException(httpEx);
                        _loggingService?.LogHttpError(ExchangeName, $"ticker/24hr?symbol={ConvertToBinanceSymbol(symbol)}", $"HTTP {statusCode}: {httpEx.Message}");
                        
                        if (statusCode == 400)
                        {
                            cryptoList.Add(GetNoDataForSymbol(symbol)); // Symbol not found
                        }
                        else
                        {
                            cryptoList.Add(GetErrorDataForSymbol(symbol)); // API error
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _loggingService?.LogError(ExchangeName, $"JSON parsing error for {symbol}: {jsonEx.Message}");
                        cryptoList.Add(GetErrorDataForSymbol(symbol));
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

        private int ExtractStatusCodeFromException(HttpRequestException ex)
        {
            var message = ex.Message;
            if (message.Contains("400")) return 400;
            if (message.Contains("401")) return 401;
            if (message.Contains("403")) return 403;
            if (message.Contains("404")) return 404;
            if (message.Contains("429")) return 429;
            if (message.Contains("500")) return 500;
            if (message.Contains("502")) return 502;
            if (message.Contains("503")) return 503;
            return 0; // Unknown
        }

        private string ConvertToBinanceSymbol(string symbol)
        {
            // Handle special cases for Binance symbol mapping
            var mappings = new Dictionary<string, string>
            {
                ["BTC"] = "BTCUSDT",
                ["ETH"] = "ETHUSDT", 
                ["BNB"] = "BNBUSDT",
                ["XRP"] = "XRPUSDT",
                ["SOL"] = "SOLUSDT",
                ["ADA"] = "ADAUSDT",
                ["AVAX"] = "AVAXUSDT",
                ["DOGE"] = "DOGEUSDT",
                ["TRX"] = "TRXUSDT",
                ["DOT"] = "DOTUSDT"
            };

            return mappings.ContainsKey(symbol.ToUpper()) ? mappings[symbol.ToUpper()] : $"{symbol.ToUpper()}USDT";
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

    public class BinanceTicker
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("priceChange")]
        public string PriceChange { get; set; }

        [JsonProperty("priceChangePercent")]
        public string PriceChangePercent { get; set; }

        [JsonProperty("weightedAvgPrice")]
        public string WeightedAvgPrice { get; set; }

        [JsonProperty("prevClosePrice")]
        public string PrevClosePrice { get; set; }

        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; }

        [JsonProperty("lastQty")]
        public string LastQty { get; set; }

        [JsonProperty("bidPrice")]
        public string BidPrice { get; set; }

        [JsonProperty("bidQty")]
        public string BidQty { get; set; }

        [JsonProperty("askPrice")]
        public string AskPrice { get; set; }

        [JsonProperty("askQty")]
        public string AskQty { get; set; }

        [JsonProperty("openPrice")]
        public string OpenPrice { get; set; }

        [JsonProperty("highPrice")]
        public string HighPrice { get; set; }

        [JsonProperty("lowPrice")]
        public string LowPrice { get; set; }

        [JsonProperty("volume")]
        public string Volume { get; set; }

        [JsonProperty("quoteVolume")]
        public string QuoteVolume { get; set; }

        [JsonProperty("openTime")]
        public long OpenTime { get; set; }

        [JsonProperty("closeTime")]
        public long CloseTime { get; set; }

        [JsonProperty("firstId")]
        public long FirstId { get; set; }

        [JsonProperty("lastId")]
        public long LastId { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }
    }
}
