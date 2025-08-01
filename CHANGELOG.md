# Changelog

All notable changes to the BitTicker project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-08-01

### Added

- Added **ByBit** exchange support with full API integration.
- Added **export logs** functionality in the log window for saving detailed debug output.
- Enhanced **configuration window** with alphabetically sorted exchange selection dropdown.
- Added explicit **dark mode styling** fixes for dropdown list backgrounds and foregrounds.
- Both **Configuration** and **Log** windows now open centered on the desktop.
- Added dynamic version display in **About** dialog:
  - Shows git branch/tag info from build environment.
  - Appends "(dev)" suffix for builds on the main branch.
- Added complete API implementations and error handling for **Huobi**, **OKX**, **Gate.io**, and **KuCoin**.
- Refined ticker window behavior:
  - Auto-scrolls when content overflows.
  - Smooth size adjustments with user-resize persistence.
  - Ticker window remains always on top; config and log windows do not.

### Fixed

- Fixed issue with configuration change not applying immediately by forcing service recreation.
- Fixed selection dropdown contrast issues on dark themes.
- Corrected various API implementations with up-to-date endpoints and data structures.
- Removed deprecated exchanges (e.g., **Bittrex**) and updated repository accordingly.
- Improved logging messages for better debugging and diagnostics.

### Security

- Updated GitHub Actions workflows to ensure proper authorization for releases and artifact uploads.

### Miscellaneous

- Added support for drag-moving the ticker window.
- Improved tooltip content consistency across exchanges.

---

## [1.0.1] - 2025-07-31

### Added
- **Configurable API refresh interval** - Users can now set custom refresh intervals from 1 to 1440 minutes (24 hours)
- **Numeric input validation** - Refresh interval field only accepts numbers with real-time validation
- **Enhanced error handling** - API failures now display "$ERR" and "ERR%" indicators in red to warn users
- **Smart scrollbar behavior** - Horizontal scrollbar only appears when mouse hovers over window and content overflows
- **Automatic ticker scrolling** - Ticker smoothly scrolls left when window is narrower than content
- **GitHub Actions workflows** - Automated build, test, and release pipelines for CI/CD
- **Professional About dialog** - Complete author information, copyright notice, and MIT license details
- **Configuration persistence** - Refresh interval setting is saved and restored between sessions
- **Input range validation** - Refresh interval is constrained to reasonable limits (1-1440 minutes)

### Changed
- **Context menu reordering** - "Refresh" menu item moved to first position for easier access
- **Configuration window layout** - Refresh interval input positioned on same line as Cancel/Save buttons
- **Field order in configuration** - Cryptocurrencies → Refresh Interval → API Key for better user flow
- **Window dragging behavior** - Window only moves when clicking directly on ticker content, not on scrollbar
- **Error state indicators** - Distinct red color (#FF6B6B) for error states vs normal gain/loss colors
- **Timer management** - Dynamic timer recreation when refresh interval changes
- **Window sizing logic** - Improved content-aware sizing that respects user manual resizing

### Fixed
- **Scrollbar clicking** - Fixed issue where clicking scrollbar would move the window
- **Configuration loading** - Proper initialization of refresh interval from saved settings
- **Mouse interaction** - Auto-scrolling pauses correctly when mouse hovers over window
- **Layout updates** - Window height adjusts properly based on scrollbar visibility
- **Input validation** - Comprehensive numeric-only input with clipboard support
- **Timer updates** - Refresh interval changes take effect immediately without restart

### Technical Improvements
- **Enhanced debug logging** - Better tracking of configuration changes and API states
- **Memory management** - Proper timer disposal and resource cleanup
- **Error recovery** - Graceful fallback from API errors to error indicators
- **Validation layers** - Multiple levels of input validation for robustness
- **Configuration migration** - Automatic correction of invalid saved settings

### User Experience
- **Intuitive interface** - More logical field ordering in configuration dialog
- **Visual feedback** - Clear error indicators when API is unavailable
- **Flexible timing** - Customizable refresh rates for different usage patterns
- **Smooth animations** - 20 FPS auto-scrolling for fluid ticker movement
- **Professional appearance** - Enhanced About dialog with proper attribution

### Developer Experience
- **CI/CD pipeline** - Automated builds for every commit and pull request
- **Automated releases** - GitHub Actions create releases with downloadable binaries
- **Code quality** - CodeQL security analysis and dependency management
- **Issue templates** - Structured bug reports and feature request forms

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

[1.0.1]: https://github.com/hvmonteiro/BitTicker/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/hvmonteiro/BitTicker/releases/tag/v1.0.0
