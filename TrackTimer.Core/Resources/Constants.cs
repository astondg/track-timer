namespace TrackTimer.Core.Resources
{
    using System.Collections.Generic;

    public static class Constants
    {
        public const string APP_LAPVERIFICATION_HASHKEY = "/LfTvl1UFe6wSKhnw1bchnVBfOKQtZKlrrg6qdeU2JA=";
        public const string APP_LAPVERIFICATION_PREFIX = "3Ml8MZw5vxZv0KF6ubKTHw==";
        public const double APP_MINIMUMLAPLENGTH_FACTOR = 0.98;

        public const double KNOTS_TO_METRES_PER_SECOND = 0.514444444;
        public const double KMH_TO_METRES_PER_SECOND = 3.6;
        public const double MILES_TO_KILOMETRES = 1.609344;
        public const double MILLIMETRES_TO_INCHES = 2.54;

        public const string SHORT_POSITIVE_TIME_FORMAT = @"\+m\:ss\.fff";
        public const string SHORT_NEGATIVE_TIME_FORMAT = @"\-m\:ss\.fff";
        public const string LONG_POSITIVE_TIME_FORMAT = @"mm\:ss\.fff";
        public const string MICROSOFT_BINGMAPS_CLIENTID = "7d3c82f9-3807-4d5e-9057-3182a35e3deb";
        public const string MICROSOFT_BINGMAPS_AUTHENTICATIONTOKEN = "eyaWxuQY50mF6YkMCkmKYg";
        public const string MICROSOFT_LIVE_CLIENTID = "000000004C0FFABA";
        public const string MICROSOFT_LIVE_SCOPE_BASIC = "wl.basic";
        public const string MICROSOFT_LIVE_SCOPE_SIGNIN = "wl.signin";
        public const string MICROSOFT_LIVE_SCOPE_OFFLINEACCESS = "wl.offline_access";
        public const string MICROSOFT_LIVE_SCOPE_EMAILS = "wl.emails";
        public const string MICROSOFT_LIVE_SCOPE_SKYDRIVE = "wl.skydrive";
        public const string MICROSOFT_LIVE_SCOPE_SKYDRIVEUPDATE = "wl.skydrive_update";
        public const string SKYDRIVE_FOLDER_NAME = "TrackTimer_Data";
        public const string AZURE_MOBILESERVICES_PATH = "https://tracktimer.azure-mobile.net/";
        public const string AZURE_MOBILESERVICES_KEY = "NKoXdPlucuOdqkDzbJEHPHEpZXavSK46";
        public const string WEATHERUNDERGROUND_API_KEY = "c45746e80ec3415b";
        public const string WEATHERUNDERGROUND_CONDITIONS_PATH_FORMAT = "http://api.wunderground.com/api/{0}/conditions/q/{1},{2}.json";
        public const string WEATHERUNDERGROUND_HISTORY_PATH_FORMAT = "http://api.wunderground.com/api/{0}/history{1}/q/{2}/{3}.json";
        public const string WEATHERUNDERGROUND_ICONS_PATH_FORMAT = "http://icons.wxug.com/i/c/i/{0}.gif";

        public static IDictionary<VehicleClass, string> VehicleClasses
        {
            get
            {
                return new Dictionary<VehicleClass, string>
                {
                    { VehicleClass.Standard, CoreResources.Enum_VehicleClass_Standard },
                    { VehicleClass.LightModifications, CoreResources.Enum_VehicleClass_LightlyModified },
                    { VehicleClass.HeavyModifications, CoreResources.Enum_VehicleClass_HeavilyModified },
                    { VehicleClass.RacePrepared, CoreResources.Enum_VehicleClass_RacePrepared }
                };
            }
        }
    }
}