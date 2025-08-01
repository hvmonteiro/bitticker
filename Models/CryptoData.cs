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
using System.ComponentModel;

namespace BitTicker
{
    public class CryptoData : INotifyPropertyChanged
    {
        private string _symbol = string.Empty;
        private decimal _price;
        private decimal _change;
        private decimal _changePercent;
        private string _name = string.Empty;
        private decimal _marketCap;
        private decimal _volume24h;
        private bool _isErrorState;
        private bool _isNoDataState;

        // Additional API fields for tooltip display
        private decimal _openPrice;
        private decimal _highPrice;
        private decimal _lowPrice;
        private decimal _bidPrice;
        private decimal _askPrice;
        private decimal _quoteVolume;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private string _exchangeName = string.Empty;

        public string Symbol
        {
            get => _symbol;
            set
            {
                _symbol = value ?? string.Empty;
                OnPropertyChanged(nameof(Symbol));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value ?? string.Empty;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string ExchangeName
        {
            get => _exchangeName;
            set
            {
                _exchangeName = value ?? string.Empty;
                OnPropertyChanged(nameof(ExchangeName));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
                OnPropertyChanged(nameof(DisplayPrice));
            }
        }

        public decimal Change
        {
            get => _change;
            set
            {
                _change = value;
                OnPropertyChanged(nameof(Change));
                OnPropertyChanged(nameof(PriceColor));
            }
        }

        public decimal ChangePercent
        {
            get => _changePercent;
            set
            {
                _changePercent = value;
                OnPropertyChanged(nameof(ChangePercent));
                OnPropertyChanged(nameof(DisplayChangePercent));
            }
        }

        public decimal MarketCap
        {
            get => _marketCap;
            set
            {
                _marketCap = value;
                OnPropertyChanged(nameof(MarketCap));
            }
        }

        public decimal Volume24h
        {
            get => _volume24h;
            set
            {
                _volume24h = value;
                OnPropertyChanged(nameof(Volume24h));
            }
        }

        public decimal OpenPrice
        {
            get => _openPrice;
            set
            {
                _openPrice = value;
                OnPropertyChanged(nameof(OpenPrice));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal HighPrice
        {
            get => _highPrice;
            set
            {
                _highPrice = value;
                OnPropertyChanged(nameof(HighPrice));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal LowPrice
        {
            get => _lowPrice;
            set
            {
                _lowPrice = value;
                OnPropertyChanged(nameof(LowPrice));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal BidPrice
        {
            get => _bidPrice;
            set
            {
                _bidPrice = value;
                OnPropertyChanged(nameof(BidPrice));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal AskPrice
        {
            get => _askPrice;
            set
            {
                _askPrice = value;
                OnPropertyChanged(nameof(AskPrice));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public decimal QuoteVolume
        {
            get => _quoteVolume;
            set
            {
                _quoteVolume = value;
                OnPropertyChanged(nameof(QuoteVolume));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public bool IsErrorState
        {
            get => _isErrorState;
            set
            {
                _isErrorState = value;
                OnPropertyChanged(nameof(IsErrorState));
                OnPropertyChanged(nameof(DisplayPrice));
                OnPropertyChanged(nameof(DisplayChangePercent));
                OnPropertyChanged(nameof(PriceColor));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public bool IsNoDataState
        {
            get => _isNoDataState;
            set
            {
                _isNoDataState = value;
                OnPropertyChanged(nameof(IsNoDataState));
                OnPropertyChanged(nameof(DisplayPrice));
                OnPropertyChanged(nameof(DisplayChangePercent));
                OnPropertyChanged(nameof(PriceColor));
                OnPropertyChanged(nameof(TooltipData));
            }
        }

        public string DisplayPrice
        {
            get
            {
                if (IsErrorState)
                    return "$ERR";
                if (IsNoDataState)
                    return "$0";
                return $"${Price:N2}";
            }
        }

        public string DisplayChangePercent
        {
            get
            {
                if (IsErrorState)
                    return "ERR%";
                if (IsNoDataState)
                    return "0%";
                
                // API already returns percentage values (e.g., "-1.658" = -1.658%)
                if (ChangePercent >= 0)
                    return $"+{ChangePercent:F2}%";
                else
                    return $"{ChangePercent:F2}%";
            }
        }

        public string PriceColor
        {
            get
            {
                if (IsErrorState)
                    return "#FFA500"; // Orange color for API error state (changed from red)
                if (IsNoDataState)
                    return "#FFA500"; // Orange color for no data state
                return ChangePercent >= 0 ? "#00FF00" : "#FF0000";
            }
        }

        public string TooltipData
        {
            get
            {
                if (IsErrorState)
                {
                    return $"API Error\n\nSymbol: {Symbol}\nExchange: {ExchangeName}\nStatus: API endpoint returned an error\nLast Update: {(LastUpdateTime != DateTime.MinValue ? LastUpdateTime.ToString("HH:mm:ss") : "N/A")}";
                }

                if (IsNoDataState)
                {
                    return $"No Data Available\n\nSymbol: {Symbol}\nExchange: {ExchangeName}\nStatus: Symbol not found or no data returned\nLast Update: {(LastUpdateTime != DateTime.MinValue ? LastUpdateTime.ToString("HH:mm:ss") : "N/A")}";
                }

                return $"{Name} ({Symbol})\nExchange: {ExchangeName}\n\n" +
                       $"Current Price: ${Price:N2}\n" +
                       $"24h Change: ${Change:N2} ({DisplayChangePercent})\n\n" +
                       $"24h High: ${HighPrice:N2}\n" +
                       $"24h Low: ${LowPrice:N2}\n" +
                       $"24h Open: ${OpenPrice:N2}\n\n" +
                       $"Bid Price: ${BidPrice:N2}\n" +
                       $"Ask Price: ${AskPrice:N2}\n\n" +
                       $"24h Volume: {Volume24h:N0} {Symbol}\n" +
                       $"24h Quote Vol: ${QuoteVolume:N0}\n\n" +
                       $"Market Cap: {(MarketCap > 0 ? $"${MarketCap:N0}" : "N/A")}\n" +
                       $"Last Update: {(LastUpdateTime != DateTime.MinValue ? LastUpdateTime.ToString("HH:mm:ss") : "N/A")}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
