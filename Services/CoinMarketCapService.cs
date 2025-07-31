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
    public class CoinMarketCapService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/";

        public CoinMarketCapService(string apiKey = "")
        {
            _apiKey = apiKey;
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
                    // Return demo data if no API key is provided
                    return GetDemoData(symbols);
                }

                var symbolsString = string.Join(",", symbols);
                var url = $"{BaseUrl}quotes/latest?symbol={symbolsString}&convert=USD";

                var response = await _httpClient.GetStringAsync(url);
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
                                Volume24h = quote.Volume24h
                            });
                        }
                    }
                }

                return cryptoList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching crypto data: {ex.Message}");
                
                // Return error data instead of demo data when API fails
                return GetErrorData(symbols);
            }
        }

        private List<CryptoData> GetDemoData(List<string> symbols)
        {
            var random = new Random();
            var demoData = new List<CryptoData>();

            var predefinedPrices = new Dictionary<string, decimal>
            {
                ["BTC"] = 43250.00m,
                ["ETH"] = 2580.00m,
                ["BNB"] = 315.50m,
                ["XRP"] = 0.62m,
                ["SOL"] = 98.75m,
                ["ADA"] = 0.48m,
                ["AVAX"] = 37.20m,
                ["DOGE"] = 0.087m,
                ["TRX"] = 0.105m,
                ["DOT"] = 7.85m
            };

            foreach (var symbol in symbols)
            {
                var basePrice = predefinedPrices.ContainsKey(symbol) ? predefinedPrices[symbol] : (decimal)(random.NextDouble() * 100);
                var changePercent = (decimal)((random.NextDouble() - 0.5) * 10); // ±5% change
                var change = basePrice * (changePercent / 100);

                demoData.Add(new CryptoData
                {
                    Symbol = symbol,
                    Name = GetCryptoName(symbol),
                    Price = basePrice + change,
                    Change = change,
                    ChangePercent = changePercent,
                    MarketCap = basePrice * (decimal)(random.NextDouble() * 1000000000),
                    Volume24h = basePrice * (decimal)(random.NextDouble() * 10000000),
                    IsErrorState = false
                });
            }

            return demoData;
        }

        private List<CryptoData> GetErrorData(List<string> symbols)
        {
            var errorData = new List<CryptoData>();

            foreach (var symbol in symbols)
            {
                errorData.Add(new CryptoData
                {
                    Symbol = symbol,
                    Name = GetCryptoName(symbol),
                    Price = 0, // Will display as "ERR"
                    Change = 0,
                    ChangePercent = 0, // Will display as "ERR%"
                    MarketCap = 0,
                    Volume24h = 0,
                    IsErrorState = true // Flag to indicate error state
                });
            }

            return errorData;
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

    // API Response Models
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
