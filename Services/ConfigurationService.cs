/*
Copyright (c) 2025 Hugo Monteiro
Licensed under the MIT License. See LICENSE file in the project root for license information.
*/

using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BitTicker
{
    public static class ConfigurationService
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BitTicker",
            "config.json"
        );

        public static Configuration LoadConfiguration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LOADING CONFIGURATION FROM: {ConfigFilePath} ===");
                
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    System.Diagnostics.Debug.WriteLine($"Raw JSON content: {json}");
                    
                    var config = JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded exchange from file: '{config.SelectedExchangeApi}'");
                    
                    // Validate and fix refresh interval if needed
                    if (config.RefreshIntervalMinutes < 1 || config.RefreshIntervalMinutes > 1440)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid refresh interval {config.RefreshIntervalMinutes} found in config, using default");
                        config.RefreshIntervalMinutes = 5; // Default to 5 minutes
                    }
                    
                    // Validate exchange selection
                    if (string.IsNullOrEmpty(config.SelectedExchangeApi) || !ExchangeInfo.Exchanges.Contains(config.SelectedExchangeApi))
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid exchange '{config.SelectedExchangeApi}' found in config, using default");
                        config.SelectedExchangeApi = ExchangeInfo.CoinMarketCap;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Final validated exchange: '{config.SelectedExchangeApi}'");
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
                System.Diagnostics.Debug.WriteLine($"=== SAVING CONFIGURATION TO: {ConfigFilePath} ===");
                System.Diagnostics.Debug.WriteLine($"Exchange to save: '{configuration.SelectedExchangeApi}'");
                
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.Diagnostics.Debug.WriteLine($"JSON to write: {json}");
                
                File.WriteAllText(ConfigFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"Configuration saved successfully");
                
                // Verify the save worked
                var verifyJson = File.ReadAllText(ConfigFilePath);
                System.Diagnostics.Debug.WriteLine($"File verification - content: {verifyJson}");
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
    }
}
