using System;
using System.Threading.Tasks;
using BugSense.Core;
using BugSense.Core.Model;
using Microsoft.WindowsAzure.MobileServices;
using TrackTimer.Resources;

namespace TrackTimer.Extensions
{
    public static class BugSenseExtensions
    {
        public static async Task<string> LogExceptionWithId(this BugSenseHandlerBase handler, MobileServiceUser user, Exception ex, params CrashExtraData[] extraCrashData)
        {
            string errorId = Guid.NewGuid().ToString();
            handler.UserIdentifier = user.UserId;
            handler.AddCrashExtraData(new CrashExtraData(AppConstants.BUGSENSE_EXTRADATA_ERRORID, errorId));

            if (extraCrashData != null)
                foreach (var crashData in extraCrashData)
                    handler.AddCrashExtraData(crashData);

            await handler.LogExceptionAsync(ex);
            return errorId;
        }
    }
}