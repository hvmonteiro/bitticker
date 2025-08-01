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
    public class KuCoinService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.kucoin.com/api/v1/market/";

        public string ExchangeName => ExchangeInfo.KuCoin;
        public bool RequiresApiKey => false;

        public KuCoinService(string apiKey = "", ILoggingService? loggingService = null)
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
                        var kuCoinSymbol = ConvertToKuCoinSymbol(symbol);
                        var statsUrl = $"{BaseUrl}stats?symbol={kuCoinSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", statsUrl);
                        var response = await _httpClient.GetStringAsync(statsUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var apiResponse = JsonConvert.DeserializeObject<KuCoinResponse>(response);
                        
                        if (apiResponse?.Code == "200000" && apiResponse.Data != null)
                        {
                            var data = apiResponse.Data;
                            
                            if (decimal.TryParse(data.Last, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                price > 0)
                            {
                                // Parse other values
                                var changePercent = decimal.TryParse(data.ChangeRate, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal chRate) ? chRate * 100 : 0; // KuCoin returns decimal, we need percentage
                                var change = decimal.TryParse(data.ChangePrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal chPrice) ? chPrice : 0;
                                var high = decimal.TryParse(data.High, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(data.Low, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                                var volume = decimal.TryParse(data.Vol, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var quoteVolume = decimal.TryParse(data.VolValue, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;
                                var bidPrice = decimal.TryParse(data.Buy, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                                var askPrice = decimal.TryParse(data.Sell, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                                var openPrice = price - change; // Calculate open price
                                
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
                                    LastUpdateTime = DateTime.Now,
                                    IsErrorState = false,
                                    IsNoDataState = false
                                });
                                
                                _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${price:F2} ({changePercent:F2}%) - Data parsed successfully");
                            }
                            else
                            {
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price data '{data.Last}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            var errorMsg = apiResponse?.Msg ?? "Unknown error";
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: API returned code {apiResponse?.Code}: {errorMsg}");
                            
                            if (apiResponse?.Code == "400100") // Invalid symbol
                            {
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                            else
                            {
                                cryptoList.Add(GetErrorDataForSymbol(symbol));
                            }
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _loggingService?.LogHttpError(ExchangeName, $"stats?symbol={ConvertToKuCoinSymbol(symbol)}", httpEx.Message);
                        
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

        private string ConvertToKuCoinSymbol(string symbol)
        {
            // KuCoin uses BTC-USDT format (uppercase with dash)
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

    public class KuCoinResponse
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("data")]
        public KuCoinData? Data { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }
    }

    public class KuCoinData
    {
        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("buy")]
        public string? Buy { get; set; } // Best bid price

        [JsonProperty("sell")]
        public string? Sell { get; set; } // Best ask price

        [JsonProperty("changeRate")]
        public string? ChangeRate { get; set; } // 24h change rate as decimal

        [JsonProperty("changePrice")]
        public string? ChangePrice { get; set; } // 24h change in price

        [JsonProperty("high")]
        public string? High { get; set; } // 24h high

        [JsonProperty("low")]
        public string? Low { get; set; } // 24h low

        [JsonProperty("vol")]
        public string? Vol { get; set; } // 24h volume in base currency

        [JsonProperty("volValue")]
        public string? VolValue { get; set; } // 24h volume in quote currency

        [JsonProperty("last")]
        public string? Last { get; set; } // Last traded price

        [JsonProperty("averagePrice")]
        public string? AveragePrice { get; set; }

        [JsonProperty("takerFeeRate")]
        public string? TakerFeeRate { get; set; }

        [JsonProperty("makerFeeRate")]
        public string? MakerFeeRate { get; set; }

        [JsonProperty("takerCoefficient")]
        public string? TakerCoefficient { get; set; }

        [JsonProperty("makerCoefficient")]
        public string? MakerCoefficient { get; set; }
    }
}
