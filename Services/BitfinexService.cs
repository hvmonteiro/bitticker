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

namespace BitTicker
{
    public class BitfinexService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private const string BaseUrl = "https://api-pub.bitfinex.com/v2/";

        public string ExchangeName => ExchangeInfo.Bitfinex;
        public bool RequiresApiKey => false;

        public BitfinexService(string apiKey = "", ILoggingService? loggingService = null)
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
                        var bitfinexSymbol = ConvertToBitfinexSymbol(symbol);
                        var tickerUrl = $"{BaseUrl}ticker/t{bitfinexSymbol}";
                        
                        _loggingService?.LogHttpRequest(ExchangeName, "GET", tickerUrl);
                        var response = await _httpClient.GetStringAsync(tickerUrl);
                        _loggingService?.LogHttpResponse(ExchangeName, 200, $"{response.Length} bytes");
                        
                        var ticker = JsonConvert.DeserializeObject<decimal[]>(response);
                        
                        if (ticker != null && ticker.Length >= 10)
                        {
                            var price = ticker[6]; // Last price
                            var volume = ticker[7]; // Volume
                            var high = ticker[8]; // High
                            var low = ticker[9]; // Low
                            var change = ticker[4]; // Daily change
                            var changePercent = ticker[5]; // Daily change percent

                            if (price > 0)
                            {
                                cryptoList.Add(new CryptoData
                                {
                                    Symbol = symbol,
                                    Name = GetCryptoName(symbol),
                                    Price = price,
                                    Change = change,
                                    ChangePercent = changePercent * 100, // Bitfinex returns as decimal
                                    Volume24h = volume,
                                    HighPrice = high,
                                    LowPrice = low,
                                    OpenPrice = price - change,
                                    BidPrice = ticker[0], // Bid
                                    AskPrice = ticker[2], // Ask
                                    QuoteVolume = 0,
                                    MarketCap = 0,
                                    ExchangeName = ExchangeName,
                                    LastUpdateTime = DateTime.Now,
                                    IsErrorState = false,
                                    IsNoDataState = false
                                });
                                
                                _loggingService?.LogInfo(ExchangeName, $"{symbol}: ${price:F2} ({changePercent * 100:F2}%)");
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
                    catch (Exception ex)
                    {
                        _loggingService?.LogError(ExchangeName, $"Error processing {symbol}: {ex.Message}");
                        cryptoList.Add(GetErrorDataForSymbol(symbol));
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

        private string ConvertToBitfinexSymbol(string symbol)
        {
            return $"{symbol.ToUpper()}USD";
        }

        private CryptoData GetErrorDataForSymbol(string symbol)
        {
            return new CryptoData { Symbol = symbol, Name = GetCryptoName(symbol), ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true };
        }

        private CryptoData GetNoDataForSymbol(string symbol)
        {
            return new CryptoData { Symbol = symbol, Name = GetCryptoName(symbol), ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsNoDataState = true };
        }

        private List<CryptoData> GetErrorData(List<string> symbols) => symbols.Select(GetErrorDataForSymbol).ToList();

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
}
