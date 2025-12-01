using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class WeatherService
    {
        private readonly string apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
        private readonly string baseUrl = Environment.GetEnvironmentVariable("WEATHER_API_URL");

        public async Task<WeatherResponse?> GetCurrentWeatherAsync(string location)
        {
            using (var client = new HttpClient())
            {
                var url = $"{baseUrl}?key={apiKey}&q={location}&aqi=no";

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<WeatherResponse>(json);
            }
        }
    }

}
