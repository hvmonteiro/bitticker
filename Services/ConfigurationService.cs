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
using System.IO;
using System.Text.Json;

namespace StockTicker
{
    public static class ConfigurationService
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StockTicker",
            "config.json"
        );

        public static Configuration LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
                    
                    // Validate and fix refresh interval if needed
                    if (config.RefreshIntervalMinutes < 1 || config.RefreshIntervalMinutes > 1440)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid refresh interval {config.RefreshIntervalMinutes} found in config, using default");
                        config.RefreshIntervalMinutes = 5; // Default to 5 minutes
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Configuration loaded: {config.CryptoCurrencies.Count} cryptos, {config.RefreshIntervalMinutes}min refresh");
                    return config;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("Creating new default configuration");
            return new Configuration();
        }

        public static void SaveConfiguration(Configuration configuration)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(ConfigFilePath, json);
                System.Diagnostics.Debug.WriteLine($"Configuration saved: {configuration.CryptoCurrencies.Count} cryptos, {configuration.RefreshIntervalMinutes}min refresh, API key: {(!string.IsNullOrEmpty(configuration.CoinMarketCapApiKey) ? "SET" : "EMPTY")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
    }
}
