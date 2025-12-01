using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class WeatherResponse
    {
        public WeatherLocation location { get; set; }
        public WeatherCurrent current { get; set; }
    }

    public class WeatherLocation
    {
        public string name { get; set; }
        public string region { get; set; }
        public string country { get; set; }
    }

    public class WeatherCurrent
    {
        public float temp_c { get; set; }
        public WeatherCondition condition { get; set; }
        public float wind_kph { get; set; }
        public int humidity { get; set; }
        public double precip_mm { get; set; }
    }

    public class WeatherCondition
    {
        public string text { get; set; }
        public string icon { get; set; } // URL icon
    }

}
