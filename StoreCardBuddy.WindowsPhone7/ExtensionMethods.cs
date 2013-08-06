using System.IO;
using System.Threading.Tasks;

namespace ClubcardManager
{
    public static class ExtensionMethods
    {
        public async static Task<Stream> ToStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(str);
            await writer.FlushAsync();
            stream.Position = 0;
            return stream;
        }
    }
}
