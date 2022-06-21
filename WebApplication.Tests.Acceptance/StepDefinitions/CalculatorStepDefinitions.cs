namespace TodoApi.Tests.Acceptance.StepDefinitions
{
    [Binding]
    public sealed class CalculatorStepDefinitions
    {
        private WeatherForecast? sut;

        [Given("a weather forecast with a temperature of (.*) celsius")]
        public void GivenAWeatherForecastWithATemperatureOfCelsius(int number)
        {
            sut = new WeatherForecast
            {
                TemperatureC = number
            };
        }

        [Then("the temperature in fahrenheit should be (.*)")]
        public void TheTemperatureInFahrenheitShouldBe(int result)
        {
            sut!.TemperatureF.Should().Be(result);
        }
    }
}