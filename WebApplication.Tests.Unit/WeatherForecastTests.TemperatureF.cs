using FluentAssertions;

namespace TodoApi.Tests.Unit;

public partial class WeatherForecastTests
{
    public class TemperatureF
    {
        [Fact]
        public void Should_EqualTemperatureC_When_TemperatureIsMinus40()
        {
            const int intersectingTemperature = -40;
            var sut = new WeatherForecast
            {
                TemperatureC = intersectingTemperature
            };

            sut.TemperatureF.Should().Be(intersectingTemperature);
        }
    }
}
