namespace TrackTimer.Core.Models
{
    public class WeatherConditions
    {
        public string Condition { get; set; }
        public double? Temperature { get; set; }
        public int? WindDegrees { get; set; }
        public double? WindSpeed { get; set; }
        public double? PreviousHourPrecipitation { get; set; }
        public double? TotalDayPrecipitation { get; set; }
    }
}