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
    public class GateIOService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.gateio.ws/api/v4/spot/";

        public string ExchangeName => ExchangeInfo.GateIO;
        public bool RequiresApiKey => false;

        public GateIOService(string apiKey = "", ILoggingService? loggingService = null)
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

                // Gate.io allows getting all tickers at once, then filter
                var tickersUrl = $"{BaseUrl}tickers";
                
                _loggingService?.LogHttpRequest(ExchangeName, "GET", tickersUrl);
                var response = await _httpClient.GetStringAsync(tickersUrl);
                _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                
                var tickers = JsonConvert.DeserializeObject<List<GateIOTicker>>(response);
                
                if (tickers != null)
                {
                    // Create symbol mapping for lookup
                    var gateIOSymbols = symbols.Select(ConvertToGateIOSymbol).ToList();
                    var symbolMap = new Dictionary<string, string>();
                    for (int i = 0; i < symbols.Count && i < gateIOSymbols.Count; i++)
                    {
                        symbolMap[gateIOSymbols[i]] = symbols[i];
                    }
                    
                    foreach (var symbol in symbols)
                    {
                        var gateIOSymbol = ConvertToGateIOSymbol(symbol);
                        var ticker = tickers.FirstOrDefault(t => t.CurrencyPair?.Equals(gateIOSymbol, StringComparison.OrdinalIgnoreCase) == true);
                        
                        if (ticker != null)
                        {
                            if (decimal.TryParse(ticker.Last, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                price > 0)
                            {
                                // Parse other values
                                var changePercent = decimal.TryParse(ticker.ChangePercentage, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal chPct) ? chPct : 0;
                                var high = decimal.TryParse(ticker.High24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(ticker.Low24h, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;
                                var volume = decimal.TryParse(ticker.BaseVolume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var quoteVolume = decimal.TryParse(ticker.QuoteVolume, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal qVol) ? qVol : 0;
                                var bidPrice = decimal.TryParse(ticker.HighestBid, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal bid) ? bid : 0;
                                var askPrice = decimal.TryParse(ticker.LowestAsk, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal ask) ? ask : 0;
                                
                                // Calculate change from percentage and current price
                                var absoluteChange = price * (changePercent / 100);
                                var openPrice = price - absoluteChange; // Calculate open price
                                
                                cryptoList.Add(new CryptoData
                                {
                                    Symbol = symbol,
                                    Name = GetCryptoName(symbol),
                                    Price = price,
                                    Change = absoluteChange,
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
                                _loggingService?.LogWarning(ExchangeName, $"{symbol}: Invalid price data '{ticker.Last}'");
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: Symbol not found in ticker data");
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                }
                else
                {
                    _loggingService?.LogError(ExchangeName, "Failed to deserialize ticker response");
                    cryptoList.AddRange(symbols.Select(GetErrorDataForSymbol));
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

        private string ConvertToGateIOSymbol(string symbol)
        {
            // Gate.io uses BTC_USDT format (uppercase with underscore)
            return $"{symbol.ToUpper()}_USDT";
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

    public class GateIOTicker
    {
        [JsonProperty("currency_pair")]
        public string? CurrencyPair { get; set; }

        [JsonProperty("last")]
        public string? Last { get; set; }

        [JsonProperty("lowest_ask")]
        public string? LowestAsk { get; set; }

        [JsonProperty("highest_bid")]
        public string? HighestBid { get; set; }

        [JsonProperty("change_percentage")]
        public string? ChangePercentage { get; set; }

        [JsonProperty("base_volume")]
        public string? BaseVolume { get; set; }

        [JsonProperty("quote_volume")]
        public string? QuoteVolume { get; set; }

        [JsonProperty("high_24h")]
        public string? High24h { get; set; }

        [JsonProperty("low_24h")]
        public string? Low24h { get; set; }

        [JsonProperty("etf_net_value")]
        public string? EtfNetValue { get; set; }

        [JsonProperty("etf_pre_net_value")]
        public string? EtfPreNetValue { get; set; }

        [JsonProperty("etf_pre_timestamp")]
        public long? EtfPreTimestamp { get; set; }

        [JsonProperty("etf_leverage")]
        public string? EtfLeverage { get; set; }
    }
}
