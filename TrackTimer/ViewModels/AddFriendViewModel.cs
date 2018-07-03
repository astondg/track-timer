using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BugSense;
using BugSense.Core.Model;
using GalaSoft.MvvmLight.Command;
using TrackTimer.Core.Models;
using TrackTimer.Core.ViewModels;
using Windows.Foundation;
using TrackTimer.Core.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using TrackTimer.Extensions;

namespace TrackTimer.ViewModels
{
    public class AddFriendViewModel : BaseViewModel
    {
        private bool canAddFriend;
        private string userNameSearchTerm;
        private ICommand addFriendCommand;
        private IEnumerable<FriendViewModel> existingFriends;

        public AddFriendViewModel(IEnumerable<FriendViewModel> existingFriends)
        {
            UserSearchResults = new ObservableCollection<User>();
            this.existingFriends = existingFriends;
            canAddFriend = true;
            this.PropertyChanged += AddFriendViewModel_PropertyChanged;
        }

        public string UserNameSearchTerm
        {
            get { return userNameSearchTerm; }
            set { SetProperty(ref userNameSearchTerm, value); }
        }

        public bool CanOperateOnFriendCollection
        {
            get { return canAddFriend; }
            set { SetProperty(ref canAddFriend, value); }
        }

        public event TypedEventHandler<AddFriendViewModel, FriendViewModel> FriendAdded;

        public ICommand AddFriend
        {
            get
            {
                if (addFriendCommand == null)
                {
                    addFriendCommand = new RelayCommand<User>(async user =>
                    {
                        if (string.Equals(user.UserId, App.MobileService.CurrentUser.UserId))
                            return;

                        try
                        {
                            CanOperateOnFriendCollection = false;
                            FriendViewModel addedFriend = await CreateFriendRequestOnServer(user);

                            if (FriendAdded != null && addedFriend != null)
                                FriendAdded(this, addedFriend);
                        }
                        finally
                        {
                            CanOperateOnFriendCollection = true;
                        }
                    }, user => CanOperateOnFriendCollection);
                }

                return addFriendCommand;
            }
        }

        public ObservableCollection<User> UserSearchResults { get; set; }

        public static async Task<FriendViewModel> CreateFriendRequestOnServer(User user)
        {
            FriendViewModel addedFriend = null;
            Exception friendException = null;

            try
            {
                var friend = new Friend { UserId = user.UserId };
                var friendsTable = App.MobileService.GetTable<Friend>();
                await friendsTable.InsertAsync(friend);
                addedFriend = friend.AsViewModel();
            }
            catch (Exception ex)
            {
                friendException = ex;
            }

            if (friendException != null)
            {
                var errorData = new LimitedCrashExtraDataList();
                errorData.Add("Message", string.Format("Failed to create Friend relationship between {0} & {1}",
                                                       App.MobileService.CurrentUser.UserId, user.UserId));
                BugSenseHandler.Instance.UserIdentifier = App.MobileService.CurrentUser.UserId;
                await BugSenseHandler.Instance.LogExceptionAsync(friendException, errorData);
            }
            return addedFriend;
        }

        private async void AddFriendViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                UserSearchResults.Clear();
                if (!string.Equals(e.PropertyName, "UserNameSearchTerm")
                    || string.IsNullOrWhiteSpace(UserNameSearchTerm) || UserNameSearchTerm.Length < 2)
                    return;

                var userTable = App.MobileService.GetTable<User>();
                var matchingUsers = await userTable.Where(u => u.FirstName == UserNameSearchTerm)
                                                   .ToEnumerableAsync();

                foreach (var user in matchingUsers)
                {
                    if (existingFriends.Any(f => f.UserId.Equals(user.UserId)))
                        continue;
                    UserSearchResults.Add(user);
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                var crashData = new List<CrashExtraData>
                {
                    new CrashExtraData("userEmail", App.ViewModel.UserProfile.EmailAddress)
                };
                if (ex.Response != null)
                {
                    crashData.Add(new CrashExtraData("responseStatusCode", ex.Response.StatusCode.ToString()));
                }
                if (ex.Response?.Content != null)
                {
                    crashData.Add(new CrashExtraData("response", await ex.Response.Content.ReadAsStringAsync()));
                }
                string errorId = await BugSenseHandler.Instance.LogExceptionWithId(App.MobileService.CurrentUser, ex, crashData.ToArray());
            }
        }
    }
}
