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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace BitTicker
{
    public partial class LogWindow : Window, INotifyPropertyChanged
    {
        private readonly ILoggingService _loggingService;
        
        public ObservableCollection<LogEntryViewModel> LogEntries { get; set; }

        public LogWindow(ILoggingService loggingService)
        {
            InitializeComponent();
            _loggingService = loggingService;
            LogEntries = new ObservableCollection<LogEntryViewModel>();
            DataContext = this;

            // Subscribe to logging events
            _loggingService.LogEntryAdded += OnLogEntryAdded;
            
            // Initial status
            UpdateStatus();
        }

        private void OnLogEntryAdded(object sender, LogEntry logEntry)
        {
            // Update UI on the main thread
            Dispatcher.BeginInvoke(() =>
            {
                var logEntryVM = new LogEntryViewModel(logEntry);
                LogEntries.Add(logEntryVM);

                // Limit log entries to prevent memory issues (keep last 1000)
                if (LogEntries.Count > 1000)
                {
                    LogEntries.RemoveAt(0);
                }

                UpdateStatus();

                // Auto-scroll to bottom if enabled
                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollViewer.ScrollToBottom();
                }
            });
        }
		
		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new Microsoft.Win32.SaveFileDialog
			{
				FileName = $"BitTicker_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
				Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
			};

			if (dlg.ShowDialog() == true)
			{
				try
				{
					using var writer = new System.IO.StreamWriter(dlg.FileName);
					foreach(var entry in LogEntries)
					{
						writer.WriteLine(entry.FormattedMessage);
					}
					MessageBox.Show("Log exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to export log: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}


        private void UpdateStatus()
        {
            StatusText.Text = LogEntries.Count > 0 ? "Logging active" : "Ready - Log messages will appear here";
            LogCountText.Text = $"{LogEntries.Count} message{(LogEntries.Count == 1 ? "" : "s")}";
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            LogEntries.Clear();
            UpdateStatus();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Hide(); // Hide instead of close so we can show it again
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Hide instead of closing so we can show the window again
            e.Cancel = true;
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_loggingService != null)
            {
                _loggingService.LogEntryAdded -= OnLogEntryAdded;
            }
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LogEntryViewModel
    {
        private readonly LogEntry _logEntry;

        public LogEntryViewModel(LogEntry logEntry)
        {
            _logEntry = logEntry;
        }

        public string FormattedMessage => _logEntry.FormattedMessage;

        public string LevelColor
        {
            get
            {
                return _logEntry.Level switch
                {
                    LogLevel.Error => "#FF6B6B",      // Red
                    LogLevel.Warning => "#FFA500",    // Orange
                    LogLevel.Info => "#87CEEB",       // Sky Blue
                    LogLevel.Debug => "#90EE90",      // Light Green
                    _ => "#CCCCCC"                    // Light Gray
                };
            }
        }
    }
}
