namespace TrackTimer.Core.ViewModels
{
    public class WeatherConditionsViewModel : BaseViewModel
    {
        private int? windDegrees;
        private double? temperature;
        private double? windSpeed;
        private double? previousHourPrecipitation;
        private double? totalDayPrecipitation;
        private string condition;

        public int? WindDirection
        {
            get { return windDegrees; }
            set { SetProperty(ref windDegrees, value); }
        }
        public double? Temperature
        {
            get { return temperature; }
            set { SetProperty(ref temperature, value); }
        }
        public double? WindSpeed
        {
            get { return windSpeed; }
            set { SetProperty(ref windSpeed, value); }
        }
        public double? PreviousHourPrecipitation
        {
            get { return previousHourPrecipitation; }
            set { SetProperty(ref previousHourPrecipitation, value); }
        }
        public double? TotalDayPrecipitation
        {
            get { return totalDayPrecipitation; }
            set { SetProperty(ref totalDayPrecipitation, value); }
        }
        public string Condition
        {
            get { return condition; }
            set { SetProperty(ref condition, value); }
        }
    }
}