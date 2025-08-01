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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockTicker
{
    public class CoinMarketCapService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILoggingService _loggingService;
        private const string BaseUrl = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/";

        public string ExchangeName => ExchangeInfo.CoinMarketCap;
        public bool RequiresApiKey => true;

        public CoinMarketCapService(string apiKey = "", ILoggingService loggingService = null)
        {
            _apiKey = apiKey;
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);
            }
        }

        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _loggingService?.LogError(ExchangeName, "No API key provided - CoinMarketCap requires API key");
                    return GetErrorData(symbols);
                }

                _loggingService?.LogInfo(ExchangeName, $"Starting data fetch for {symbols.Count} symbols");

                var symbolsString = string.Join(",", symbols);
                var url = $"{BaseUrl}quotes/latest?symbol={symbolsString}&convert=USD";

                _loggingService?.LogHttpRequest(ExchangeName, "GET", url);

                var response = await _httpClient.GetStringAsync(url);
                
                _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");

                var apiResponse = JsonConvert.DeserializeObject<CoinMarketCapResponse>(response);

                var cryptoList = new List<CryptoData>();

                foreach (var symbol in symbols)
                {
                    if (apiResponse?.Data?.ContainsKey(symbol) == true)
                    {
                        var coinData = apiResponse.Data[symbol];
                        var quote = coinData.Quote?.USD;

                        if (quote != null)
                        {
                            cryptoList.Add(new CryptoData
                            {
                                Symbol = coinData.Symbol,
                                Name = coinData.Name,
                                Price = quote.Price,
                                Change = quote.Price * (quote.PercentChange24h / 100),
                                ChangePercent = quote.PercentChange24h,
                                MarketCap = quote.MarketCap,
                                Volume24h = quote.Volume24h,
                                OpenPrice = 0, // Not available in CoinMarketCap API
                                HighPrice = 0, // Not available in CoinMarketCap API
                                LowPrice = 0, // Not available in CoinMarketCap API
                                BidPrice = 0, // Not available in CoinMarketCap API
                                AskPrice = 0, // Not available in CoinMarketCap API
                                QuoteVolume = 0, // Not available in CoinMarketCap API
                                LastUpdateTime = DateTime.Now,
                                ExchangeName = ExchangeName,
                                IsErrorState = false,
                                IsNoDataState = false
                            });
                            
                            _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${quote.Price:F2} ({quote.PercentChange24h:F2}%) - Data parsed successfully");
                        }
                        else
                        {
                            _loggingService?.LogWarning(ExchangeName, $"{symbol}: Data exists but quote is null");
                            cryptoList.Add(GetNoDataForSymbol(symbol));
                        }
                    }
                    else
                    {
                        _loggingService?.LogWarning(ExchangeName, $"{symbol}: Symbol not found in API response");
                        cryptoList.Add(GetNoDataForSymbol(symbol));
                    }
                }

                var successCount = cryptoList.Count(c => !c.IsErrorState && !c.IsNoDataState);
                var errorCount = cryptoList.Count(c => c.IsErrorState);
                var noDataCount = cryptoList.Count(c => c.IsNoDataState);
                
                _loggingService?.LogInfo(ExchangeName, $"Data fetch completed: {successCount} successful, {errorCount} errors, {noDataCount} no data");

                return cryptoList.Count > 0 ? cryptoList : GetErrorData(symbols);
            }
            catch (HttpRequestException httpEx)
            {
                _loggingService?.LogHttpError(ExchangeName, "quotes/latest", httpEx.Message);
                return GetErrorData(symbols);
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(ExchangeName, $"API Error: {ex.Message}");
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
                MarketCap = 0,
                Volume24h = 0,
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
                MarketCap = 0,
                Volume24h = 0,
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

    // API Response Models remain the same
    public class CoinMarketCapResponse
    {
        [JsonProperty("data")]
        public Dictionary<string, CoinData> Data { get; set; }
    }

    public class CoinData
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("quote")]
        public QuoteData Quote { get; set; }
    }

    public class QuoteData
    {
        [JsonProperty("USD")]
        public UsdQuote USD { get; set; }
    }

    public class UsdQuote
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("percent_change_24h")]
        public decimal PercentChange24h { get; set; }

        [JsonProperty("market_cap")]
        public decimal MarketCap { get; set; }

        [JsonProperty("volume_24h")]
        public decimal Volume24h { get; set; }
    }
}
