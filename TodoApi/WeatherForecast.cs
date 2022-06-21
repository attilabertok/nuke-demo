
namespace TodoApi;
public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(9d * TemperatureC / 5);

    public string? Summary { get; set; }
}
