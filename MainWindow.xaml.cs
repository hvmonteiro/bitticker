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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace StockTicker
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer? _sizeCheckTimer;
        private DispatcherTimer? _scrollTimer;
        private bool _isMouseOver = false;
        private double _contentWidth = 0;
        private double _optimalWidth = 0;
        private bool _shouldAutoScroll = false;
        private double _scrollSpeed = 30; // pixels per second
        private double _currentScrollPosition = 0;
        private LogWindow? _logWindow;
        private readonly ILoggingService _loggingService;

        public MainWindow()
        {
            InitializeComponent();
            
            // Create logging service
            _loggingService = new LoggingService();
            
            // Create view model with logging service
            _viewModel = new MainViewModel(_loggingService);
            DataContext = _viewModel;
            
            // Timer to check scrollbar visibility and window sizing
            _sizeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _sizeCheckTimer.Tick += (s, e) => UpdateWindowSizeAndScrollbar();
            
            // Timer for automatic scrolling
            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            _scrollTimer.Tick += ScrollTimer_Tick;
            
            LoadWindowSettings();
            Loaded += (s, e) => _sizeCheckTimer.Start();
        }

        private void LoadWindowSettings()
        {
            var config = _viewModel.Configuration;
            
            if (config.WindowLeft > 0 && config.WindowTop > 0)
            {
                Left = config.WindowLeft;
                Top = config.WindowTop;
            }
            
            // Don't automatically restore width - let it size to content first
            // Width will be set based on content in UpdateWindowSizeAndScrollbar
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!_shouldAutoScroll || _isMouseOver) return;

            try
            {
                var maxScroll = MainScrollViewer.ScrollableWidth;
                if (maxScroll <= 0) return;

                // Move scroll position
                _currentScrollPosition += _scrollSpeed * 0.05; // 50ms interval

                // Reset to beginning when we reach the end
                if (_currentScrollPosition > maxScroll + 100) // Add pause at the end
                {
                    _currentScrollPosition = -100; // Start from before the beginning for smooth transition
                }

                // Apply scroll position (clamp to valid range)
                var scrollPos = Math.Max(0, Math.Min(_currentScrollPosition, maxScroll));
                MainScrollViewer.ScrollToHorizontalOffset(scrollPos);
            }
            catch
            {
                // Ignore scrolling exceptions
            }
        }

        private void UpdateWindowSizeAndScrollbar()
        {
            if (CryptoItemsControl == null || MainScrollViewer == null) return;

            try
            {
                // Force layout update
                MainScrollViewer.UpdateLayout();
                CryptoItemsControl.UpdateLayout();

                // Get the natural content width
                _contentWidth = CryptoItemsControl.DesiredSize.Width;
                
                // Calculate optimal window width (content + borders + padding)
                var borderWidth = 2; // 1px left + 1px right border
                var resizeGripWidth = 10; // resize grip width
                _optimalWidth = _contentWidth + borderWidth + resizeGripWidth;

                // Ensure minimum width
                _optimalWidth = Math.Max(_optimalWidth, MinWidth);

                var currentViewportWidth = MainScrollViewer.ViewportWidth;
                
                // Determine if we need scrolling
                bool needsScrolling = _contentWidth > currentViewportWidth;

                // If mouse is not over window, size to content and hide scrollbar
                if (!_isMouseOver)
                {
                    MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    
                    // Size window to optimal width, but don't exceed current width if user manually resized
                    var savedWidth = _viewModel.Configuration.WindowWidth;
                    if (savedWidth > 0 && savedWidth < _optimalWidth)
                    {
                        // User has manually made window smaller, keep it that size
                        Width = savedWidth;
                        
                        // Start auto-scrolling if content doesn't fit
                        _shouldAutoScroll = needsScrolling;
                        if (_shouldAutoScroll)
                        {
                            _scrollTimer.Start();
                        }
                        else
                        {
                            _scrollTimer.Stop();
                            _currentScrollPosition = 0;
                        }
                    }
                    else
                    {
                        // Size to content - no scrolling needed
                        Width = _optimalWidth;
                        _shouldAutoScroll = false;
                        _scrollTimer.Stop();
                        _currentScrollPosition = 0;
                    }
                    
                    Height = 45; // Compact height without scrollbar
                    MaxHeight = 45;
                }
                else
                {
                    // Mouse is over - stop auto-scrolling and show scrollbar if needed
                    _shouldAutoScroll = false;
                    _scrollTimer.Stop();
                    
                    if (Width < _contentWidth + borderWidth)
                    {
                        MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        Height = 60; // Height with scrollbar
                        MaxHeight = 60;
                    }
                    else
                    {
                        MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        Height = 45; // Compact height
                        MaxHeight = 45;
                    }
                }
            }
            catch
            {
                // Ignore layout exceptions
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOver = true;
            UpdateWindowSizeAndScrollbar();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseOver = false;
            UpdateWindowSizeAndScrollbar();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOver = true;
            UpdateWindowSizeAndScrollbar();
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            // Check if mouse is still within window bounds
            var position = e.GetPosition(this);
            var windowBounds = new Rect(0, 0, ActualWidth, ActualHeight);
            
            if (!windowBounds.Contains(position))
            {
                _isMouseOver = false;
                UpdateWindowSizeAndScrollbar();
            }
        }

        private void CryptoItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only allow window dragging when clicking on the ticker content itself
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                    e.Handled = true;
                }
                catch 
                { 
                    // Ignore any drag move exceptions
                }
            }
        }

        private void ResizeGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't handle - allow normal resize behavior
            e.Handled = true; // Prevent window dragging when clicking resize grip
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Update scrollbar visibility when scroll changes
            Dispatcher.BeginInvoke(() => UpdateWindowSizeAndScrollbar(), DispatcherPriority.Background);
        }

        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update when content size changes
            Dispatcher.BeginInvoke(() => UpdateWindowSizeAndScrollbar(), DispatcherPriority.Background);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.RefreshDataAsync();
        }

        private async void Configure_Click(object sender, RoutedEventArgs e)
        {
            var currentExchange = _viewModel.Configuration.SelectedExchangeApi;
            _loggingService.LogInfo("UI", $"=== OPENING CONFIGURATION WINDOW ===");
            _loggingService.LogInfo("UI", $"Current exchange before config: '{currentExchange}'");
            
            var configWindow = new ConfigurationWindow(_viewModel.Configuration);
            var dialogResult = configWindow.ShowDialog();
            
            _loggingService.LogInfo("UI", $"Configuration dialog result: {dialogResult}");
            
            if (dialogResult == true)
            {
                var newExchange = _viewModel.Configuration.SelectedExchangeApi;
                _loggingService.LogInfo("UI", $"Configuration was saved. Exchange: '{currentExchange}' -> '{newExchange}'");
                
                // CRITICAL: Force ViewModel to reload configuration from file
                _loggingService.LogInfo("UI", "FORCING configuration reload...");
                _viewModel.LoadConfiguration();
                
                // Verify the change took effect
                var actualExchange = _viewModel.Configuration.SelectedExchangeApi;
                _loggingService.LogInfo("UI", $"After reload, exchange is: '{actualExchange}'");
                
                // Force immediate data refresh
                _loggingService.LogInfo("UI", "FORCING data refresh...");
                await _viewModel.RefreshDataAsync();
                
                // Update window size after content changes
                Dispatcher.BeginInvoke(() => UpdateWindowSizeAndScrollbar(), DispatcherPriority.Background);
                
                _loggingService.LogInfo("UI", $"=== CONFIGURATION COMPLETE ===");
            }
            else
            {
                _loggingService.LogInfo("UI", "Configuration dialog was cancelled");
            }
        }

        private void ShowLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_logWindow == null)
            {
                _logWindow = new LogWindow(_loggingService);
                // Don't set Owner so it can go behind other applications
                // _logWindow.Owner = this;  // Removed to prevent topmost behavior
            }

            _logWindow.Show();
            _logWindow.Activate();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (_viewModel?.Configuration != null)
            {
                _viewModel.Configuration.WindowLeft = Left;
                _viewModel.Configuration.WindowTop = Top;
                _viewModel.SaveConfiguration();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_viewModel?.Configuration != null)
            {
                // Only save width if it was manually changed by user (not programmatically)
                if (e.PreviousSize.Width > 0 && Math.Abs(e.NewSize.Width - _optimalWidth) > 5)
                {
                    _viewModel.Configuration.WindowWidth = Width;
                    _viewModel.SaveConfiguration();
                }
            }

            if (e.WidthChanged)
            {
                UpdateWindowSizeAndScrollbar();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _sizeCheckTimer?.Stop();
            _scrollTimer?.Stop();
            _logWindow?.Close();
            base.OnClosed(e);
        }
    }
}
