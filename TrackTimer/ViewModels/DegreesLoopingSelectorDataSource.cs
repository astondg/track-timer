namespace TrackTimer.ViewModels
{
    using System;
    using Microsoft.Phone.Controls.Primitives;

    public class DegreesLoopingSelectorDataSource : ILoopingSelectorDataSource
    {
        private double selectedItem;

        public object GetNext(object relativeTo)
        {
            double? value = relativeTo as double?;
            if (!value.HasValue) return relativeTo;
            return value.Value < 359
                    ? value.Value + 1
                    : 0.0;
        }

        public object GetPrevious(object relativeTo)
        {
            double? value = relativeTo as double?;
            if (!value.HasValue) return relativeTo;
            return value.Value > 0
                    ? value.Value - 1
                    : 359.0;
        }

        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                double oldValue = selectedItem;
                selectedItem = (double)value;
                if (SelectionChanged != null)
                    SelectionChanged(this, new System.Windows.Controls.SelectionChangedEventArgs(new[] { oldValue }, new[] { selectedItem }));
            }
        }

        public event EventHandler<System.Windows.Controls.SelectionChangedEventArgs> SelectionChanged;
    }
}