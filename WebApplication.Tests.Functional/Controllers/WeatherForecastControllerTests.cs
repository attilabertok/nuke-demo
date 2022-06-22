namespace TodoApi.Tests.Functional.Controllers;

public partial class WeatherForecastControllerTests
{
    private static readonly HttpClient Client = new() { BaseAddress = new Uri("https://localhost:52867") };
}
