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

namespace StockTicker
{
    public class CryptoComService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.crypto.com/exchange/v1/";

        public string ExchangeName => ExchangeInfo.CryptoCom;
        public bool RequiresApiKey => false;

        public CryptoComService(string apiKey = "", ILoggingService? loggingService = null)
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

                // Process each symbol individually with GET requests
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var cryptoComSymbol = ConvertToCryptoComSymbol(symbol);
                        
                        // Use the correct GET endpoint according to official docs
                        var tickerUrl = $"{BaseUrl}public/get-tickers?instrument_name={cryptoComSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        
                        var response = await _httpClient.GetStringAsync(tickerUrl);
                        
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var apiResponse = JsonConvert.DeserializeObject<CryptoComResponse>(response);
                        
                        if (apiResponse?.Code == 0 && apiResponse.Result?.Data?.Count > 0)
                        {
                            var ticker = apiResponse.Result.Data.First();
                            
                            // Parse according to official API field names
                            if (decimal.TryParse(ticker.A, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                price > 0)
                            {
                                // Parse other values using official field names
                                var change = decimal.TryParse(ticker.C, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ch) ? ch : 0;
                                var high = decimal.TryParse(ticker.H, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(ticker.L, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                                var volume = decimal.TryParse(ticker.V, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var bidPrice = decimal.TryParse(ticker.B, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                                var askPrice = decimal.TryParse(ticker.K, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                                var quoteVolume = decimal.TryParse(ticker.Vv, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;
                                
                                // Calculate percentage change from absolute change and current price
                                var changePercent = price > 0 ? (change / price) * 100 : 0;
                                var openPrice = price - change; // Calculate open price
                                
                                // Parse timestamp
                                var lastUpdateTime = DateTime.Now;
                                if (ticker.T > 0)
                                {
                                    try
                                    {
                                        lastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(ticker.T).DateTime.ToLocalTime();
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
                                    QuoteVolume = quoteVolume,
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
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price data '{ticker.A}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            var errorMsg = apiResponse?.Message ?? "Unknown error";
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: API returned code {apiResponse?.Code}: {errorMsg}");
                            cryptoList.Add(GetNoDataForSymbol(symbol)); // Treat API errors as no data for individual symbols
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _loggingService?.LogHttpError(ExchangeName, $"public/get-tickers?instrument_name={ConvertToCryptoComSymbol(symbol)}", httpEx.Message);
                        
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

        private string ConvertToCryptoComSymbol(string symbol)
        {
            // Crypto.com Exchange uses spot format like BTC_USDT
            var mappings = new Dictionary<string, string>
            {
                ["BTC"] = "BTC_USDT",
                ["ETH"] = "ETH_USDT", 
                ["BNB"] = "BNB_USDT",
                ["XRP"] = "XRP_USDT",
                ["SOL"] = "SOL_USDT",
                ["ADA"] = "ADA_USDT",
                ["AVAX"] = "AVAX_USDT",
                ["DOGE"] = "DOGE_USDT",
                ["TRX"] = "TRX_USDT",
                ["DOT"] = "DOT_USDT"
            };

            return mappings.ContainsKey(symbol.ToUpper()) ? mappings[symbol.ToUpper()] : $"{symbol.ToUpper()}_USDT";
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

    // Response Models based on official documentation
    public class CryptoComResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("method")]
        public string? Method { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("result")]
        public CryptoComResult? Result { get; set; }
    }

    public class CryptoComResult
    {
        [JsonProperty("data")]
        public List<CryptoComTicker>? Data { get; set; }
    }

    public class CryptoComTicker
    {
        [JsonProperty("h")]
        public string? H { get; set; } // Price of the 24h highest trade

        [JsonProperty("l")]
        public string? L { get; set; } // Price of the 24h lowest trade

        [JsonProperty("a")]
        public string? A { get; set; } // The price of the latest trade

        [JsonProperty("i")]
        public string? I { get; set; } // Instrument name

        [JsonProperty("v")]
        public string? V { get; set; } // The total 24h traded volume

        [JsonProperty("vv")]
        public string? Vv { get; set; } // The total 24h traded volume value (in USD)

        [JsonProperty("oi")]
        public string? Oi { get; set; } // Open interest

        [JsonProperty("c")]
        public string? C { get; set; } // 24-hour price change

        [JsonProperty("b")]
        public string? B { get; set; } // The current best bid price

        [JsonProperty("k")]
        public string? K { get; set; } // The current best ask price

        [JsonProperty("t")]
        public long T { get; set; } // Trade timestamp
    }
}
