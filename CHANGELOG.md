# Changelog

All notable changes to the BitTicker project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-07-31

### Added
- Initial release of BitTicker
- Real-time cryptocurrency price ticker using CoinMarketCap API
- Configurable cryptocurrency selection
- Always-on-top draggable window interface
- Dynamic window height adjustment based on content
- Smart scrollbar visibility (only shows when needed)
- Right-click context menu with configuration and refresh options
- Persistent settings storage (window position, size, selected cryptos)
- Demo mode support (works without API key)
- 5-minute automatic refresh interval
- Manual refresh capability
- Color-coded price changes (green for gains, red for losses)
- MIT license
- Comprehensive documentation

### Features
- **Supported Cryptocurrencies**: BTC, ETH, BNB, XRP, SOL, ADA, AVAX, DOGE, TRX, DOT (default)
- **Configuration**: Easy-to-use settings dialog
- **API Integration**: CoinMarketCap Professional API support
- **Responsive Design**: Auto-adjusting height and scrollbar behavior
- **Persistence**: Saves window position, size, and cryptocurrency preferences

### Technical Details
- Built with WPF and .NET 6
- MVVM architecture pattern
- JSON configuration storage
- HTTP REST API integration
- Cross-platform compatible (Windows focus)

[1.0.0]: https://github.com/hvmonteiro/BitTicker/releases/tag/v1.0.0
