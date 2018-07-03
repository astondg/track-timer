namespace TrackTimer.Extensions
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Windows.Storage;

    public static class SerializationExtensions
    {
        private static JsonSerializerSettings serialisationSettings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };

        public static async Task<T> ReadJsonFileAs<T>(this StorageFile file) where T : class
        {
            using (var fileStream = await file.OpenReadAsync())
            {
                if (fileStream.Size == 0)
                    return null;

                var serializer = JsonSerializer.Create(serialisationSettings);
                using (var reader = new StreamReader(fileStream.AsStreamForRead()))
                using (var jsonReader = new JsonTextReader(reader))
                    return serializer.Deserialize<T>(jsonReader);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static T ReadJsonStreamAs<T>(this Stream stream) where T : class
        {
            if (stream.Length == 0)
                return null;

            var serializer = JsonSerializer.Create(serialisationSettings);
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
                return serializer.Deserialize<T>(jsonReader);
        }

        public static async Task WriteJsonToFile<T>(this StorageFile file, T item)
        {
            var serializer = JsonSerializer.Create(serialisationSettings);
            using (var stream = await file.OpenStreamForWriteAsync())
            using (var writer = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(writer))
                serializer.Serialize(jsonWriter, item, typeof(T));
        }
    }
}