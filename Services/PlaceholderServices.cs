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

namespace StockTicker
{
    // Placeholder services for unimplemented exchanges
    public class HuobiService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.Huobi;
        public bool RequiresApiKey => false;

        public HuobiService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }

    public class KuCoinService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.KuCoin;
        public bool RequiresApiKey => false;

        public KuCoinService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }

    public class BittrexService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.Bittrex;
        public bool RequiresApiKey => false;

        public BittrexService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }

    public class BitstampService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.Bitstamp;
        public bool RequiresApiKey => false;

        public BitstampService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }

    public class GateIOService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.GateIO;
        public bool RequiresApiKey => false;

        public GateIOService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }

    public class OKXService : ICryptoExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        public string ExchangeName => ExchangeInfo.OKX;
        public bool RequiresApiKey => false;

        public OKXService(string apiKey = "", ILoggingService? loggingService = null) 
        { 
            _loggingService = loggingService;
            _httpClient = new HttpClient(); 
        }
        
        public async Task<List<CryptoData>> GetCryptocurrencyDataAsync(List<string> symbols)
        {
            _loggingService?.LogWarning(ExchangeName, "API not yet implemented");
            await Task.Delay(1);
            return GetErrorData(symbols);
        }
        
        private List<CryptoData> GetErrorData(List<string> symbols) => 
            symbols.Select(s => new CryptoData { Symbol = s, ExchangeName = ExchangeName, LastUpdateTime = DateTime.Now, IsErrorState = true }).ToList();
            
        public void Dispose() => _httpClient?.Dispose();
    }
}
