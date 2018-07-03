namespace TrackTimer.Extensions
{
    using System.Text;
    using Windows.Storage.Streams;

    public static class DataReaderExtensions
    {
        public static string ReadString(this DataReader dataReader, Encoding encoding, int length)
        {
            // TODO - This assumes single byte characters
            byte[] bytes = new byte[length];
            dataReader.ReadBytes(bytes);
            return encoding.GetString(bytes, 0, length);
        }
    }
}