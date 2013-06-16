using System.IO;
using Newtonsoft.Json;

namespace StoreCardBuddy
{
    public static class ExtensionMethods
    {
        public static Stream ToStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static T Clone<T>(this T source) where T : class
        {
            var json = JsonConvert.SerializeObject(source);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
