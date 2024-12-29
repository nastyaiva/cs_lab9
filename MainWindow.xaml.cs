﻿//using System.Text;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private class City
        {
            public required string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public override string ToString() => Name;
        }

        private class Weather
        {
            public string Name { get; set; } = "";
            public string Country { get; set; } = "";
            public double Temp { get; set; }
            public string Description { get; set; } = "";
            public override string ToString() =>
                $"Погода в {Name}, {Country}:\n" +
                $"Температура: {Temp:F1}°C\n" +
                $"Условия: {Description}";
        }

        private class WeatherService
        {
            private HttpClient _client;
            private string _apiKey;

            public WeatherService(string apiKey)
            {
                _client = new HttpClient();
                _apiKey = apiKey;
            }

            public async Task<Weather> GetWeatherAsync(double lat, double lon)
            {
                var response = await _client.GetStringAsync(
                    $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric");
                var json = JsonDocument.Parse(response);
                var root = json.RootElement;
                var sys = root.GetProperty("sys");
                var main = root.GetProperty("main");
                var weather = root.GetProperty("weather")[0];
                return new Weather
                {
                    Country = sys.GetProperty("country").GetString()!,
                    Name = root.GetProperty("name").GetString()!,
                    Temp = main.GetProperty("temp").GetDouble(),
                    Description = weather.GetProperty("description").GetString()!
                };
            }
        }

        private WeatherService _weatherService = null!;
        private List<City> _cities = null!;

        public MainWindow()
        {
            InitializeComponent();
            LoadCities();
        }

        private void LoadCities()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "city.txt");
            var lines = File.ReadAllLines(path);
            _cities = new List<City>();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var parts = line.Split('\t');
                if (parts.Length == 2)
                {
                    var cityName = parts[0].Trim();
                    var coords = parts[1].Split(',');
                    if (coords.Length == 2)
                    {
                        var lat = double.Parse(coords[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
                        var lon = double.Parse(coords[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
                        _cities.Add(new City
                        {
                            Name = cityName,
                            Latitude = lat,
                            Longitude = lon
                        });
                    }
                }
            }
            _weatherService = new WeatherService("9074a89f53780baef2f636c1099aea02");
            CitiesList.ItemsSource = _cities;
        }

        private async void GetWeatherButton_Click(object sender, RoutedEventArgs e)
        {
            if (CitiesList.SelectedItem == null) return;
            var selectedCity = (City)CitiesList.SelectedItem;
            GetWeatherButton.IsEnabled = false;
            WeatherInfo.Text = "Загрузка...";
            var weather = await _weatherService.GetWeatherAsync(
                selectedCity.Latitude,
                selectedCity.Longitude);
            WeatherInfo.Text = weather.ToString();
            GetWeatherButton.IsEnabled = true;
        }
    }
}