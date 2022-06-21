Feature: WeatherForecast
Weather forecast data structure

Scenario: Temperatures intersect at -40 degrees
    Given a weather forecast with a temperature of -40 celsius
    Then the temperature in fahrenheit should be -40
