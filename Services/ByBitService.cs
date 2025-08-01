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
    public class ByBitService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.bybit.com/v5/market/";

        public string ExchangeName => ExchangeInfo.ByBit;
        public bool RequiresApiKey => false;

        public ByBitService(string apiKey = "", ILoggingService? loggingService = null)
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
                        var bybitSymbol = ConvertToByBitSymbol(symbol);
                        var tickerUrl = $"{BaseUrl}tickers?category=spot&symbol={bybitSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        var response = await _httpClient.GetStringAsync(tickerUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var apiResponse = JsonConvert.DeserializeObject<ByBitResponse>(response);
                        
                        if (apiResponse?.RetCode == 0 && apiResponse.Result?.List?.Count > 0)
                        {
                            var ticker = apiResponse.Result.List.First();
                            
                            if (decimal.TryParse(ticker.LastPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                price > 0)
                            {
                                // Parse other values
                                var changePercent = decimal.TryParse(ticker.Price24hPcnt, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal chPct) ? chPct * 100 : 0; // ByBit returns as decimal
                                var high = decimal.TryParse(ticker.HighPrice24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(ticker.LowPrice24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                                var volume = decimal.TryParse(ticker.Volume24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var quoteVolume = decimal.TryParse(ticker.Turnover24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;
                                var bidPrice = decimal.TryParse(ticker.Bid1Price, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                                var askPrice = decimal.TryParse(ticker.Ask1Price, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                                
                                // Calculate change from percentage and current price
                                var change = price * (changePercent / 100);
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
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price data '{ticker.LastPrice}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            var errorMsg = apiResponse?.RetMsg ?? "Unknown error";
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: API returned code {apiResponse?.RetCode}: {errorMsg}");
                            
                            if (apiResponse?.RetCode == 10001) // Invalid symbol
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
                        _loggingService?.LogHttpError(ExchangeName, $"tickers?category=spot&symbol={ConvertToByBitSymbol(symbol)}", httpEx.Message);
                        
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

        private string ConvertToByBitSymbol(string symbol)
        {
            // ByBit uses BTCUSDT format for spot trading (uppercase, no separator)
            return $"{symbol.ToUpper()}USDT";
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

    public class ByBitResponse
    {
        [JsonProperty("retCode")]
        public int RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string? RetMsg { get; set; }

        [JsonProperty("result")]
        public ByBitResult? Result { get; set; }

        [JsonProperty("retExtInfo")]
        public object? RetExtInfo { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public class ByBitResult
    {
        [JsonProperty("category")]
        public string? Category { get; set; }

        [JsonProperty("list")]
        public List<ByBitTicker>? List { get; set; }
    }

    public class ByBitTicker
    {
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }

        [JsonProperty("lastPrice")]
        public string? LastPrice { get; set; }

        [JsonProperty("indexPrice")]
        public string? IndexPrice { get; set; }

        [JsonProperty("markPrice")]
        public string? MarkPrice { get; set; }

        [JsonProperty("prevPrice24h")]
        public string? PrevPrice24h { get; set; }

        [JsonProperty("price24hPcnt")]
        public string? Price24hPcnt { get; set; }

        [JsonProperty("highPrice24h")]
        public string? HighPrice24h { get; set; }

        [JsonProperty("lowPrice24h")]
        public string? LowPrice24h { get; set; }

        [JsonProperty("prevPrice1h")]
        public string? PrevPrice1h { get; set; }

        [JsonProperty("openInterest")]
        public string? OpenInterest { get; set; }

        [JsonProperty("openInterestValue")]
        public string? OpenInterestValue { get; set; }

        [JsonProperty("turnover24h")]
        public string? Turnover24h { get; set; }

        [JsonProperty("volume24h")]
        public string? Volume24h { get; set; }

        [JsonProperty("fundingRate")]
        public string? FundingRate { get; set; }

        [JsonProperty("nextFundingTime")]
        public string? NextFundingTime { get; set; }

        [JsonProperty("predictedDeliveryPrice")]
        public string? PredictedDeliveryPrice { get; set; }

        [JsonProperty("basisRate")]
        public string? BasisRate { get; set; }

        [JsonProperty("basis")]
        public string? Basis { get; set; }

        [JsonProperty("deliveryFeeRate")]
        public string? DeliveryFeeRate { get; set; }

        [JsonProperty("deliveryTime")]
        public string? DeliveryTime { get; set; }

        [JsonProperty("ask1Size")]
        public string? Ask1Size { get; set; }

        [JsonProperty("bid1Price")]
        public string? Bid1Price { get; set; }

        [JsonProperty("ask1Price")]
        public string? Ask1Price { get; set; }

        [JsonProperty("bid1Size")]
        public string? Bid1Size { get; set; }
    }
}
