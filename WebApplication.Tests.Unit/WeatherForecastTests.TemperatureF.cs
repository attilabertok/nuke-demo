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

        [Fact]
        public void Should_EqualTemperatureC_When_TemperatureIsMinus40Again()
        {
            const int intersectingTemperature = -40;
            var sut = new WeatherForecast
            {
                TemperatureC = intersectingTemperature
            };

            sut.TemperatureF.Should().Be(intersectingTemperature);
        }

        [Fact]
        public void Should_EqualTemperatureC_When_TemperatureIsMinus40AndAgain()
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
