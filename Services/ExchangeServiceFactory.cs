/*
Copyright (c) 2025 Hugo Monteiro
Licensed under the MIT License. See LICENSE file in the project root for license information.
*/

using System;

namespace StockTicker
{
    public static class ExchangeServiceFactory
    {
        public static ICryptoExchangeService CreateService(string exchangeName, string apiKey = "", ILoggingService? loggingService = null)
        {
            loggingService?.LogDebug("Factory", $"=== CREATING SERVICE ===");
            loggingService?.LogDebug("Factory", $"Requested exchange: '{exchangeName}'");
            loggingService?.LogDebug("Factory", $"API key provided: {(!string.IsNullOrEmpty(apiKey) ? "YES" : "NO")}");
            
            ICryptoExchangeService service;
            
            switch (exchangeName)
            {
                case ExchangeInfo.CoinMarketCap:
                    service = new CoinMarketCapService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Binance:
                    service = new BinanceService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Coinbase:
                    service = new CoinbaseService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Kraken:
                    service = new KrakenService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Bitfinex:
                    service = new BitfinexService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Huobi:
                    service = new HuobiService(apiKey, loggingService);
                    break;
                case ExchangeInfo.KuCoin:
                    service = new KuCoinService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Bittrex:
                    service = new BittrexService(apiKey, loggingService);
                    break;
                case ExchangeInfo.Bitstamp:
                    service = new BitstampService(apiKey, loggingService);
                    break;
                case ExchangeInfo.GateIO:
                    service = new GateIOService(apiKey, loggingService);
                    break;
                case ExchangeInfo.OKX:
                    service = new OKXService(apiKey, loggingService);
                    break;
                default:
                    loggingService?.LogWarning("Factory", $"Unknown exchange '{exchangeName}', defaulting to CoinMarketCap");
                    service = new CoinMarketCapService(apiKey, loggingService);
                    break;
            }
            
            loggingService?.LogInfo("Factory", $"=== SERVICE CREATED SUCCESSFULLY ===");
            loggingService?.LogInfo("Factory", $"Requested: '{exchangeName}' -> Created: '{service.ExchangeName}'");
            
            return service;
        }
    }
}
