/*
Copyright (c) 2025 Hugo Monteiro
Licensed under the MIT License. See LICENSE file in the project root for license information.
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
    public class KrakenService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api.kraken.com/0/public/";

        public string ExchangeName => ExchangeInfo.Kraken;
        public bool RequiresApiKey => false;

        public KrakenService(string apiKey = "", ILoggingService? loggingService = null)
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

                // Convert symbols to Kraken format and get ticker data
                var krakenSymbols = symbols.Select(ConvertToKrakenSymbol).ToList();
                var symbolsString = string.Join(",", krakenSymbols);
                
                var tickerUrl = $"{BaseUrl}Ticker?pair={symbolsString}";
                
                _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                var response = await _httpClient.GetStringAsync(tickerUrl);
                _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                
                var apiResponse = JsonConvert.DeserializeObject<KrakenResponse>(response);
                
                if (apiResponse?.Result != null)
                {
                    foreach (var symbol in symbols)
                    {
                        var krakenSymbol = ConvertToKrakenSymbol(symbol);
                        
                        if (apiResponse.Result.ContainsKey(krakenSymbol))
                        {
                            var ticker = apiResponse.Result[krakenSymbol];
                            
                            if (decimal.TryParse(ticker.C?[0], NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price) &&
                                decimal.TryParse(ticker.O, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal openPrice) &&
                                price > 0 && openPrice > 0)
                            {
                                var change = price - openPrice;
                                var changePercent = (change / openPrice) * 100;
                                var volume = decimal.TryParse(ticker.V?[1], NumberStyles.Float, CultureInfo.InvariantCulture, out decimal vol) ? vol : 0;
                                var high = decimal.TryParse(ticker.H?[1], NumberStyles.Float, CultureInfo.InvariantCulture, out decimal h) ? h : 0;
                                var low = decimal.TryParse(ticker.L?[1], NumberStyles.Float, CultureInfo.InvariantCulture, out decimal l) ? l : 0;

                                cryptoList.Add(new CryptoData
                                {
                                    Symbol = symbol,
                                    Name = GetCryptoName(symbol),
                                    Price = price,
                                    Change = change,
                                    ChangePercent = changePercent,
                                    Volume24h = volume,
                                    OpenPrice = openPrice,
                                    HighPrice = high,
                                    LowPrice = low,
                                    BidPrice = 0,
                                    AskPrice = 0,
                                    QuoteVolume = 0,
                                    MarketCap = 0,
                                    ExchangeName = ExchangeName,
                                    LastUpdateTime = DateTime.Now,
                                    IsErrorState = false,
                                    IsNoDataState = false
                                });
                                
                                _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${price:F2} ({changePercent:F2}%)");
                            }
                            else
                            {
                                cryptoList.Add(GetNoDataForSymbol(symbol));
                            }
                        }
                        else
                        {
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                }

                var successCount = cryptoList.Count(c => !c.IsErrorState && !c.IsNoDataState);
                _loggingService?.LogInfo(ExchangeName, $"Data fetch completed: {successCount} successful");
                return cryptoList;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(ExchangeName, $"API error: {ex.Message}");
                return GetErrorData(symbols);
            }
        }

        private string ConvertToKrakenSymbol(string symbol)
        {
            var mappings = new Dictionary<string, string>
            {
                ["BTC"] = "XXBTZUSD",
                ["ETH"] = "XETHZUSD",
                ["XRP"] = "XXRPZUSD",
                ["SOL"] = "SOLUSD",
                ["ADA"] = "ADAUSD",
                ["AVAX"] = "AVAXUSD",
                ["DOGE"] = "XDGUSD",
                ["DOT"] = "DOTUSD"
            };

            return mappings.ContainsKey(symbol.ToUpper()) ? mappings[symbol.ToUpper()] : $"{symbol}USD";
        }

        private CryptoData GetErrorDataForSymbol(string symbol)
        {
            return new CryptoData
            {
                Symbol = symbol,
                Name = GetCryptoName(symbol),
                ExchangeName = ExchangeName,
                LastUpdateTime = DateTime.Now,
                IsErrorState = true
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

    public class KrakenResponse
    {
        [JsonProperty("error")]
        public string[]? Error { get; set; }

        [JsonProperty("result")]
        public Dictionary<string, KrakenTicker>? Result { get; set; }
    }

    public class KrakenTicker
    {
        [JsonProperty("a")]
        public string[]? A { get; set; } // Ask

        [JsonProperty("b")]
        public string[]? B { get; set; } // Bid

        [JsonProperty("c")]
        public string[]? C { get; set; } // Last trade closed

        [JsonProperty("v")]
        public string[]? V { get; set; } // Volume

        [JsonProperty("p")]
        public string[]? P { get; set; } // Volume weighted average price

        [JsonProperty("t")]
        public int[]? T { get; set; } // Number of trades

        [JsonProperty("l")]
        public string[]? L { get; set; } // Low

        [JsonProperty("h")]
        public string[]? H { get; set; } // High

        [JsonProperty("o")]
        public string? O { get; set; } // Opening price
    }
}
