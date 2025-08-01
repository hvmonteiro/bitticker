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

using System.Collections.Generic;

namespace BitTicker
{
    public class Configuration
    {
        public List<string> CryptoCurrencies { get; set; } = new List<string>
        {
            "BTC", "ETH", "BNB", "XRP", "SOL", "ADA", "AVAX", "DOGE", "TRX", "DOT"
        };

        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 800;
        public DisplayMode LastDisplayMode { get; set; } = DisplayMode.All;

        // Selected exchange API - Default to Binance
        public string SelectedExchangeApi { get; set; } = ExchangeInfo.Binance;

        // Dictionary to store API keys for each exchange
        public Dictionary<string, string> ExchangeApiKeys { get; set; } = new Dictionary<string, string>();

        // Legacy property for backward compatibility (maps to CoinMarketCap key)
        public string CoinMarketCapApiKey 
        { 
            get => ExchangeApiKeys.ContainsKey(ExchangeInfo.CoinMarketCap) ? ExchangeApiKeys[ExchangeInfo.CoinMarketCap] : string.Empty;
            set 
            {
                if (!string.IsNullOrEmpty(value))
                    ExchangeApiKeys[ExchangeInfo.CoinMarketCap] = value;
                else if (ExchangeApiKeys.ContainsKey(ExchangeInfo.CoinMarketCap))
                    ExchangeApiKeys.Remove(ExchangeInfo.CoinMarketCap);
            }
        }

        private int _refreshIntervalMinutes = 5;
        public int RefreshIntervalMinutes 
        { 
            get => _refreshIntervalMinutes;
            set => _refreshIntervalMinutes = value >= 1 && value <= 1440 ? value : 5;
        }
    }

    public static class ExchangeInfo
    {
        // All supported exchanges (added ByBit)
        public const string CoinMarketCap = "CoinMarketCap";  // Default first!
        public const string Binance = "Binance";
        public const string Bitfinex = "Bitfinex";
        public const string Bitstamp = "Bitstamp";
        public const string ByBit = "ByBit";  // New exchange added
        public const string Coinbase = "Coinbase";
        public const string CryptoCom = "Crypto.com";
        public const string GateIO = "Gate.io";
        public const string Huobi = "Huobi";
        public const string Kraken = "Kraken";
        public const string KuCoin = "KuCoin";
        public const string OKX = "OKX";

        // Sorted alphabetically (except CoinMarketCap first as default)
        public static readonly string[] Exchanges = new string[]
        {
            CoinMarketCap,  // Keep as default first option
            Binance,
            Bitfinex,
            Bitstamp,
            ByBit,          // New exchange added in alphabetical order
            Coinbase,
            CryptoCom,
            GateIO,
            Huobi,
            Kraken,
            KuCoin,
            OKX
        };

        public static readonly Dictionary<string, string> ExchangeUrls = new Dictionary<string, string>
        {
            { Binance, "https://api.binance.com" },
            { Bitfinex, "https://api-pub.bitfinex.com" },
            { Bitstamp, "https://www.bitstamp.net/api" },
            { ByBit, "https://api.bybit.com" },  // New exchange URL
            { Coinbase, "https://api.exchange.coinbase.com" },
            { CryptoCom, "https://api.crypto.com" },
            { GateIO, "https://api.gateio.ws" },
            { Huobi, "https://api.huobi.pro" },
            { Kraken, "https://api.kraken.com" },
            { KuCoin, "https://api.kucoin.com" },
            { OKX, "https://www.okx.com/api" },
            { CoinMarketCap, "https://pro-api.coinmarketcap.com" }
        };
    }
}
