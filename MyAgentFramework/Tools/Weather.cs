using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MyAgentFramework.Tools
{
    internal class Weather
    {
        [Description("Get the weather for a given location.")]
        public static string GetWeather([Description("The location to get the weather for.")] string location)
            => $"The weather in {location} is cloudy with a high of 15°C.";
    }
}
