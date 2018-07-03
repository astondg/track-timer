namespace TrackTimer.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using TrackTimer.Core.ViewModels;

    public class BackgroundTransfersViewModel : BaseViewModel
    {
        private bool hasTransferRequests;

        public BackgroundTransfersViewModel()
        {
            TransferRequests = new ObservableCollection<BackgroundTransferViewModel>();
        }

        public bool HasTransferRequests
        {
            get { return hasTransferRequests; }
            set { SetProperty(ref hasTransferRequests, value); }
        }

        public ObservableCollection<BackgroundTransferViewModel> TransferRequests { get; set; }

        public async Task LoadData(bool attachUploads = true)
        {
            var pendingUploads = await App.LiveClient.GetPendingUploads();
            if (attachUploads)
            {
                foreach (var request in pendingUploads)
                {
                    var backgroundTransfer = await BackgroundTransferViewModel.FromLivePendingUpload(request);
                    TransferRequests.Add(backgroundTransfer);
                }
            }
            HasTransferRequests = pendingUploads.Any();
        }
    }
}