namespace TrackTimer.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Live;
    using Microsoft.Phone.BackgroundTransfer;
    using TrackTimer.Core.ViewModels;

    public class BackgroundTransferViewModel : BaseViewModel
    {
        private string requestId;
        private string tag;
        private string status;
        private long totalBytesToSend;
        private long bytesSent;
        private double progress;
        private bool isTransfering;

        public static async Task<BackgroundTransferViewModel> FromLivePendingUpload(LivePendingUpload transfer)
        {
            var result = new BackgroundTransferViewModel();
            var transferProgress = new Progress<LiveOperationProgress>(result.TransferProgressChanged);
            var attachResult = await transfer.AttachAsync(new CancellationToken(), transferProgress);
            if (attachResult.Result.ContainsKey("name"))
                result.Tag = attachResult.Result["name"].ToString();
            return result;
        }

        public string RequestId
        {
            get { return requestId; }
            set { SetProperty(ref requestId, value); }
        }
        public string Tag
        {
            get { return tag; }
            set { SetProperty(ref tag, value); }
        }
        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }
        public long TotalBytesToSend
        {
            get { return totalBytesToSend; }
            set { SetProperty(ref totalBytesToSend, value); }
        }
        public long BytesSent
        {
            get { return bytesSent; }
            set { SetProperty(ref bytesSent, value); }
        }
        public double Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }
        public bool IsTransfering
        {
            get { return isTransfering; }
            set { SetProperty(ref isTransfering, value); }
        }

        private void TransferProgressChanged(LiveOperationProgress progress)
        {
            if (!IsTransfering)
            {
                IsTransfering = true;
                Status = Resources.AppResources.Text_Blurb_BackgroundTransferTransfering;
            }

            BytesSent = progress.BytesTransferred;
            TotalBytesToSend = progress.TotalBytes;
            Progress = progress.ProgressPercentage;
            if (progress.ProgressPercentage == 100d)
                Status = Resources.AppResources.Text_Blurb_BackgroundTransferComplete;
        }

        private void ProcessTransferStatus(BackgroundTransferRequest transfer)
        {
            switch (transfer.TransferStatus)
            {
                case TransferStatus.Completed:
                    // If the status code of a completed transfer is 200 or 206, the
                    // transfer was successful
                    Status = (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                                ? Resources.AppResources.Text_Blurb_BackgroundTransferComplete
                                : Resources.AppResources.Text_Blurb_BackgroundTransferError;
                    break;

                case TransferStatus.Transferring:
                    IsTransfering = true;
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferTransfering;
                    break;

                case TransferStatus.Paused:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferPaused;
                    break;

                case TransferStatus.Waiting:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferWaiting;
                    break;

                case TransferStatus.WaitingForExternalPower:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferWaitingForExternalPower;
                    break;

                case TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferWaitingForExternalPowerDueToBatterySaverMode;
                    break;

                case TransferStatus.WaitingForNonVoiceBlockingNetwork:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferWaitingForNonVoiceBlockingNetwork;
                    break;

                case TransferStatus.WaitingForWiFi:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferWaitingForWifi;
                    break;

                case TransferStatus.Unknown:
                    Status = Resources.AppResources.Text_Blurb_BackgroundTransferUnknown;
                    break;
            }
        }
    }
}