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

using System.ComponentModel;

namespace StockTicker
{
    public class CryptoData : INotifyPropertyChanged
    {
        private string _symbol;
        private decimal _price;
        private decimal _change;
        private decimal _changePercent;
        private string _name;
        private decimal _marketCap;
        private decimal _volume24h;
        private bool _isErrorState;

        public string Symbol
        {
            get => _symbol;
            set
            {
                _symbol = value;
                OnPropertyChanged(nameof(Symbol));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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
            }
        }

        public string DisplayPrice => IsErrorState ? "$ERR" : $"${Price:N2}";

        public string DisplayChangePercent => IsErrorState ? "ERR%" : $"{ChangePercent:+0.00%;-0.00%;0.00%}";

        public string PriceColor
        {
            get
            {
                if (IsErrorState)
                    return "#FF6B6B"; // Red color for error state
                return ChangePercent >= 0 ? "#00FF00" : "#FF0000";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
