using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using TrackTimer.Core.ViewModels;

namespace TrackTimer.ViewModels
{
    public class ManageFriendsViewModel
    {
        public ManageFriendsViewModel(ObservableCollection<FriendViewModel> allFriends)
        {
            ConfirmedFriends = new ObservableCollection<FriendViewModel>();
            FriendRequests = new ObservableCollection<FriendViewModel>();
            allFriends.CollectionChanged += allFriends_CollectionChanged;
        }

        public ICommand AcceptFriend { get { return App.ViewModel.AcceptFriend; } }
        public ICommand DeleteFriend { get { return App.ViewModel.DeleteFriend; } }

        public ObservableCollection<FriendViewModel> ConfirmedFriends { get; set; }
        public ObservableCollection<FriendViewModel> FriendRequests { get; set; }

        private void allFriends_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var friend = item as FriendViewModel;
                    if (friend == null) continue;

                    if (friend.CanConfirmFriendship)
                        FriendRequests.Add(friend);
                    else
                        ConfirmedFriends.Add(friend);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    var friend = item as FriendViewModel;
                    if (friend == null) continue;

                    if (FriendRequests.Contains(friend))
                        FriendRequests.Remove(friend);
                    if (ConfirmedFriends.Contains(friend))
                        ConfirmedFriends.Remove(friend);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ConfirmedFriends.Clear();
                FriendRequests.Clear();
            }
        }
    }
}
