namespace TrackTimer.ViewModels
{
    using System;

    public class DoubleValueChangedEventArgs : EventArgs
    {
        public DoubleValueChangedEventArgs()
        { }

        public DoubleValueChangedEventArgs(double? oldValue, double? newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public double? OldValue { get; set; }
        public double? NewValue { get; set; }
    }
}