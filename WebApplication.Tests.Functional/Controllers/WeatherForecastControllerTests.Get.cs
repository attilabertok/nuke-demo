using System.Net.Http.Json;
using FluentAssertions;

namespace TodoApi.Tests.Functional.Controllers;

public partial class WeatherForecastControllerTests
{
    public class Get
    {
        [Fact]
        public async Task Should_ReturnFiveItems()
        {
            var forecasts = await Client.GetFromJsonAsync<List<WeatherForecast>>("/WeatherForecast");

            forecasts.Should()
                .HaveCount(5);
        }

        [Fact]
        public async Task Should_ReturnFiveItemsAgain()
        {
            var forecasts = await Client.GetFromJsonAsync<List<WeatherForecast>>("/WeatherForecast");

            forecasts.Should()
                .HaveCount(5);
        }
    }
}
