using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticKernel.Plugins;

public class WeatherPlugin(HttpClient httpClient)
{
    [KernelFunction("get_weather_by_lat_lon")]
    [Description("Gets the current weather for a given latitude and longitude.")]
    [return: Description("A JSON containing the weather at the requested latitude and longitude.")]
    public async Task<string> GetWeatherAsync(double lat, double lon)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
            
        var response = await httpClient.GetStringAsync(url);
        return response;
    }
}