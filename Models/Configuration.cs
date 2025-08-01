/*
Copyright (c) 2025 Hugo Monteiro
Licensed under the MIT License. See LICENSE file in the project root for license information.
*/

using System.Collections.Generic;

namespace StockTicker
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

        // Selected exchange API - Default to CoinMarketCap, not Coinbase!
        public string SelectedExchangeApi { get; set; } = ExchangeInfo.CoinMarketCap;

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
        // Top 10 Crypto Exchanges + CoinMarketCap
        public const string CoinMarketCap = "CoinMarketCap";  // Default first!
        public const string Binance = "Binance";
        public const string Coinbase = "Coinbase";
        public const string Kraken = "Kraken";
        public const string Bitfinex = "Bitfinex";
        public const string Huobi = "Huobi";
        public const string KuCoin = "KuCoin";
        public const string Bittrex = "Bittrex";
        public const string Bitstamp = "Bitstamp";
        public const string GateIO = "Gate.io";
        public const string OKX = "OKX";

        public static readonly string[] Exchanges = new string[]
        {
            CoinMarketCap,  // Keep as default first option
            Binance,
            Coinbase,
            Kraken,
            Bitfinex,
            Huobi,
            KuCoin,
            Bittrex,
            Bitstamp,
            GateIO,
            OKX
        };

        public static readonly Dictionary<string, string> ExchangeUrls = new Dictionary<string, string>
        {
            { Binance, "https://api.binance.com" },
            { Coinbase, "https://api.exchange.coinbase.com" },
            { Kraken, "https://api.kraken.com" },
            { Bitfinex, "https://api-pub.bitfinex.com" },
            { Huobi, "https://api.huobi.pro" },
            { KuCoin, "https://api.kucoin.com" },
            { Bittrex, "https://api.bittrex.com" },
            { Bitstamp, "https://www.bitstamp.net/api" },
            { GateIO, "https://api.gateio.ws" },
            { OKX, "https://www.okx.com/api" },
            { CoinMarketCap, "https://pro-api.coinmarketcap.com" }
        };
    }
}
