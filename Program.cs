using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

class Program
{
    private static readonly string API_KEY = "OGdjYU5YZElRQkg0QVNOZDRiRUN4YWtOb2JNNkZReGMzd1BBeDBLc1pEST0";
    private static readonly HttpClient client = new HttpClient();
    private static readonly object fileLock = new object();
    private static readonly string outputFile = "results.txt";

    static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync()
    {
        try
        {
            string[] tickers = File.ReadAllLines("ticker.txt");
            File.WriteAllText(outputFile, string.Empty);

            var fromDate = "2023-01-23";
            var toDate = "2023-12-23";

            foreach (var ticker in tickers)
            {
                if (tickers.First() != ticker)
                {
                    await Task.Delay(1000);
                }
                await ProcessTickerAsync(ticker, fromDate, toDate);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }

        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static async Task ProcessTickerAsync(string ticker, string fromDate, string toDate)
    {
        try
        {
            string url = $"https://api.marketdata.app/v1/stocks/candles/D/{ticker}/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {API_KEY}");
            var response = await client.SendAsync(request);

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<StockDataResponse>(jsonResponse);

            if (data?.Status == "ok" && data.High != null && data.Low != null && data.High.Length > 0)
            {
                double averagePrice = 0;
                int count = data.High.Length;

                for (int i = 0; i < count; i++)
                {
                    averagePrice += (data.High[i] + data.Low[i]) / 2;
                }

                averagePrice /= count;

                lock (fileLock)
                {
                    File.AppendAllText(outputFile, $"{ticker}:{averagePrice:F2}\n");
                }

                Console.WriteLine($"Обработан тикер {ticker}: средняя цена = {averagePrice:F2}");
            }
            else
            {
                Console.WriteLine($"Ошибка при обработке {ticker}: {data?.Status ?? "неизвестная ошибка"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке {ticker}: {ex.Message}");
        }
    }
}

class StockDataResponse
{
    [JsonProperty("s")]
    public string Status { get; set; }

    [JsonProperty("h")]
    public double[] High { get; set; }

    [JsonProperty("l")]
    public double[] Low { get; set; }
}

