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
    public class OKXService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://www.okx.com/api/v5/market/";

        public string ExchangeName => ExchangeInfo.OKX;
        public bool RequiresApiKey => false;

        public OKXService(string apiKey = "", ILoggingService? loggingService = null)
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

                // Process each symbol individually
                foreach (var symbol in symbols)
                {
                    try
                    {
                        var okxSymbol = ConvertToOKXSymbol(symbol);
                        var tickerUrl = $"{BaseUrl}ticker?instId={okxSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        var response = await _httpClient.GetStringAsync(tickerUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var apiResponse = JsonConvert.DeserializeObject<OKXResponse>(response);
                        
                        if (apiResponse?.Code == "0" && apiResponse.Data?.Count > 0)
                        {
                            var ticker = apiResponse.Data.First();
                            
                            if (decimal.TryParse(ticker.Last, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                price > 0)
                            {
                                // Parse other values - OKX provides comprehensive data
                                var change = decimal.TryParse(ticker.Last24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ch) ? price - ch : 0;
                                var changePercent = decimal.TryParse(ticker.Last24hPercent, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal chPct) ? chPct * 100 : 0; // OKX returns as decimal
                                var high = decimal.TryParse(ticker.High24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(ticker.Low24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                                var volume = decimal.TryParse(ticker.Vol24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var quoteVolume = decimal.TryParse(ticker.VolCcy24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;
                                var bidPrice = decimal.TryParse(ticker.BidPx, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                                var askPrice = decimal.TryParse(ticker.AskPx, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                                var openPrice = decimal.TryParse(ticker.Open24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal open) ? open : price - change;
                                
                                // Parse timestamp
                                var lastUpdateTime = DateTime.Now;
                                if (long.TryParse(ticker.Ts, out long timestamp))
                                {
                                    try
                                    {
                                        lastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.ToLocalTime();
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
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price data '{ticker.Last}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            var errorMsg = apiResponse?.Msg ?? "Unknown error";
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: API returned code {apiResponse?.Code}: {errorMsg}");
                            
                            if (apiResponse?.Code == "51001") // Invalid instId
                            {
                                cryptoList.Add(GetNoDataForSymbol(symbol)); // Symbol not available
                            }
                            else
                            {
                                cryptoList.Add(GetErrorDataForSymbol(symbol)); // API error
                            }
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _loggingService?.LogHttpError(ExchangeName, $"ticker?instId={ConvertToOKXSymbol(symbol)}", httpEx.Message);
                        
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

        private string ConvertToOKXSymbol(string symbol)
        {
            // OKX uses BTC-USDT format (uppercase with dash)
            return $"{symbol.ToUpper()}-USDT";
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

    public class OKXResponse
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }

        [JsonProperty("data")]
        public List<OKXTicker>? Data { get; set; }
    }

    public class OKXTicker
    {
        [JsonProperty("instType")]
        public string? InstType { get; set; }

        [JsonProperty("instId")]
        public string? InstId { get; set; }

        [JsonProperty("last")]
        public string? Last { get; set; } // Last traded price

        [JsonProperty("lastSz")]
        public string? LastSz { get; set; } // Last traded size

        [JsonProperty("askPx")]
        public string? AskPx { get; set; } // Best ask price

        [JsonProperty("askSz")]
        public string? AskSz { get; set; } // Best ask size

        [JsonProperty("bidPx")]
        public string? BidPx { get; set; } // Best bid price

        [JsonProperty("bidSz")]
        public string? BidSz { get; set; } // Best bid size

        [JsonProperty("open24h")]
        public string? Open24h { get; set; } // 24h open

        [JsonProperty("high24h")]
        public string? High24h { get; set; } // 24h high

        [JsonProperty("low24h")]
        public string? Low24h { get; set; } // 24h low

        [JsonProperty("vol24h")]
        public string? Vol24h { get; set; } // 24h base volume

        [JsonProperty("volCcy24h")]
        public string? VolCcy24h { get; set; } // 24h quote volume

        [JsonProperty("ts")]
        public string? Ts { get; set; } // Timestamp

        [JsonProperty("sodUtc0")]
        public string? SodUtc0 { get; set; } // Start of day UTC+0

        [JsonProperty("sodUtc8")]
        public string? SodUtc8 { get; set; } // Start of day UTC+8

        // Added properties for change calculation
        [JsonProperty("last24h")]
        public string? Last24h { get; set; } // Last price 24h ago (for change calculation)

        [JsonProperty("last24hPct")]
        public string? Last24hPercent { get; set; } // 24h percentage change
    }
}
