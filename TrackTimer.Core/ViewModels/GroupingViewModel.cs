namespace TrackTimer.Core.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class GroupingViewModel<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
    {
        public GroupingViewModel(IGrouping<TKey, TElement> unit)
        {
            Key = unit.Key;
            foreach (var item in unit)
                Add(item);
        }

        public GroupingViewModel(TKey key, IEnumerable<TElement> elements)
        {
            Key = key;
            foreach (var item in elements)
                Add(item);
        }

        public TKey Key { get; private set; }
    }
}