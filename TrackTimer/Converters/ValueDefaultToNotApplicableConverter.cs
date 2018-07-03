namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Resources;

    public class ValueDefaultToNotApplicableConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var valueType = value.GetType();
            if ((valueType.IsValueType && value.Equals(Activator.CreateInstance(valueType))) || value == null)
                return AppResources.Text_Default_NotApplicable;
            if (value is double)
                return Math.Round((double)value, 3);
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!value.Equals(AppResources.Text_Default_NotApplicable))
                return value;

            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            else
                return null;
        }
    }
}