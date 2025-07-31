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
        public string CoinMarketCapApiKey { get; set; } = "";
        
        private int _refreshIntervalMinutes = 5;
        public int RefreshIntervalMinutes 
        { 
            get => _refreshIntervalMinutes;
            set => _refreshIntervalMinutes = value >= 1 && value <= 1440 ? value : 5;
        }
    }
}
