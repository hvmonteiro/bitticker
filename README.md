# BitTicker

BitTicker is a lightweight and customizable cryptocurrency price ticker for Windows.
It supports multiple major exchanges, real-time price updates, dark mode, and a clean, unobtrusive UI.

![BitTicker](https://github.com/hvmonteiro/bitticker/blob/main/images/bitticker.png?raw=true)



## Features

### Core Features

- **Real-time cryptocurrency prices** from multiple major exchanges with customizable refresh interval
- **Customizable cryptocurrency selection** - Configure which cryptos to display
- **Always-on-top window** that stays visible above other applications
- **Instant manual refresh** - Right-click → Refresh for immediate data updates
- **Auto-refresh on configuration** - Data automatically updates when settings change

### Smart Interface Design

- **Intelligent window sizing** - Automatically adjusts to fit ticker content
- **Dynamic height adjustment** - Compact (45px) or standard (60px) based on scrollbar visibility
- **Auto-scrolling ticker** - Smoothly scrolls left when content exceeds window width
- **Mouse-aware behavior** - Auto-scrolling pauses when mouse hovers over window
- **Precision dragging** - Window  moves when clicking directly on ticker content
- **Width-only resizing** - Drag bottom-right corner to adjust width
- **Color-coded price changes** - Green for gains, Red for losses with percentage display
- **Highlight API Errors** - Error states highlight cryptos with issues (e.g., API failures or missing data)
- **Smart scrollbar visibility** - Only appears on mouse hover when content overflows
- **Smooth animations** - 20 FPS scrolling for fluid movement
- **Advanced Configuration UI:**
  - Choose exchange and input API keys where required
  - Set refresh interval (with input validation)
- **Detailed Tooltips:**
  - Real-time price, volume, 24h high/low, bid/ask, and more
  - Clear visual indicators for price changes
- **Robust Logging and Debugging:**
  - Log window with detailed informational, warning, and error messages
  - Log export button for saving logs to a text file
  - Logs automatically scroll to latest entry, with toggle option
  
### User Experience

- **Right-click context menu** with intuitive menu:
  - **Refresh** (first for quick access)
  - **Configure** (settings)
  - **About** (application info with author and license)
  - **Exit** (application control)

### Configuration & Customization

- **CoinMarketCap API integration** - Use your own API key for real data
- **Persistent settings** - Remembers window position, size, and selected cryptocurrencies
- **Flexible cryptocurrency input** - Comma-separated symbols (e.g., BTC,ETH,BNB)
- **Window position memory** - Restores last position and size on startup
- **User-controlled width** - Respects manual window resizing preferences

![BitTicker](https://github.com/hvmonteiro/bitticker/blob/main/images/bitticker-configuration.png?raw=true)

### Support for 12 Major Cryptocurrency Exchanges
  - CoinMarketCap (requires API key)
  - Binance
  - Bitfinex
  - Bitstamp
  - ByBit (new!)
  - Coinbase
  - Crypto.com
  - Gate.io
  - Huobi
  - Kraken
  - KuCoin
  - OKX

## Prerequisites

- **Windows 10/11** (x64)
- **.NET 6.0 Runtime** or later
- **CoinMarketCap API Key** (optional - demo data available without key)

## Installation

### Option 1: Download Release

1. Go to [Releases](../../releases)
2. Download the latest `BitTicker-vX.X.X.zip`
3. Extract to your desired location
4. Run `BitTicker.exe`

### Option 2: Build from Source

1. Clone the repository:
   
   ```
   git clone https://github.com/hvmonteiro/BitTicker.git
   cd BitTicker
   ```

2. Install .NET 6 SDK if not already installed:
- Download from [Microsoft .NET Downloads](https://dotnet.microsoft.com/download/dotnet/6.0)
3. Restore dependencies:
   
   ```
   dotnet restore
   ```

4. Build the application:
   
   ```
   dotnet build --configuration Release
   ```

5. Run the application:
   
   ```
   dotnet run
   ```

## Configuration

### API Key Setup (Recommended)

1. Get a free API key from [CoinMarketCap API](https://coinmarketcap.com/api/)
2. Right-click the ticker window → **Configure**
3. Enter your API key in the configuration dialog
4. Save settings

### Cryptocurrency Selection

1. Right-click the ticker window → **Configure**
2. Enter cryptocurrency symbols separated by commas (e.g., `BTC,ETH,BNB,XRP,SOL`)
3. Click **Save**

### Default Cryptocurrencies

- Bitcoin (BTC)
- Ethereum (ETH)
- BNB (BNB)
- XRP (XRP)
- Solana (SOL)
- Cardano (ADA)
- Avalanche (AVAX)
- Dogecoin (DOGE)
- TRON (TRX)
- Polkadot (DOT)

*All cryptocurrencies are fully customizable through the configuration dialog.*

## Usage

### Basic Operations

- **Move window**: Left-click and drag anywhere on the ticker
- **Configure**: Right-click → Configure
- **Refresh data**: Right-click → Refresh
- **Resize width**: Drag the bottom-right corner
- **Exit**: Right-click → Exit

### Keyboard Shortcuts

- **Right-click**: Context menu
- **Alt+F4**: Close application

## Technical Details

### Built With

- **Framework**: WPF (.NET 6)
- **Language**: C#
- **API**: CoinMarketCap Professional API
- **Data Format**: JSON
- **Architecture**: MVVM Pattern

### Project Structure

```
BitTicker/
├── Models/
│ ├── Configuration.cs # Configuration model
│ └── CryptoData.cs # Cryptocurrency data model
├── ViewModels/
│ └── MainViewModel.cs # Main application logic
├── Services/
│ ├── ConfigurationService.cs # Settings persistence
│ └── CoinMarketCapService.cs # API integration
├── Windows/
│ ├── ConfigurationWindow.xaml # Settings dialog
│ └── AboutWindow.xaml # About dialog
├── MainWindow.xaml # Main ticker window
└── App.xaml # Application entry point
```

### Configuration File

Settings are stored in: `%AppData%\BitTicker\config.json`

```
{
"CryptoCurrencies": ["BTC", "ETH", "BNB"],
"WindowLeft": 100,
"WindowTop": 100,
"WindowWidth": 800,
"CoinMarketCapApiKey": "your-api-key-here"
}
```

## Development

### Prerequisites for Development

- **Visual Studio 2022** or **Visual Studio Code**
- **.NET 6.0 SDK**
- **Git**

### Development Setup

1. Fork the repository
2. Clone your fork:
   
   ```
   git clone https://github.com/yourusername/BitTicker.git
   ```
3. Open in Visual Studio or VS Code
4. Build and run with F5

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

## Troubleshooting

### Common Issues

**Application won't start**

- Ensure .NET 6.0 Runtime is installed
- Check Windows Defender/Antivirus settings

**No price data showing**

- Verify internet connection
- Check API key validity in configuration
- Try using demo mode (remove API key)

**Window positioning issues**

- Delete configuration file: `%AppData%\BitTicker\config.json`
- Restart application

**High CPU usage**

- Check network connectivity to CoinMarketCap API
- Verify API key hasn't exceeded rate limits

## API Rate Limits for CoinMarketCap

### Free Tier (No API Key)

- Uses demo data only
- No rate limits
- Simulated price changes

### CoinMarketCap Free Plan

- 333 calls/day
- 10,000 calls/month
- Updates every 5 minutes = ~288 calls/day

### CoinMarketCap Paid Plans

- Higher rate limits available
- Real-time data options
- Professional features

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Hugo Monteiro**

- GitHub: [@hvmonteiro](https://github.com/hvmonteiro)

## Acknowledgments

- **CoinMarketCap** for providing cryptocurrency data API
- **Microsoft** for .NET and WPF framework
- **Newtonsoft.Json** for JSON serialization

## Version History

### v1.0.0 (2025-07-31)

- Initial release
- Real-time cryptocurrency ticker
- CoinMarketCap API integration
- Configurable interface
- Persistent settings

---

**Support the Project**: If you find BitTicker useful, consider starring the repository and sharing it with others, or you can [Sponsor](https://github.com/sponsors/hvmonteiro) me!
