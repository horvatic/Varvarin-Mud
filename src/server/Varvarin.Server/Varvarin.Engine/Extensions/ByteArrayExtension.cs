using System.Text;

namespace Varvarin.Engine.Extensions
{
    public static class ByteArrayExtension
    {
        public static string ConvertToString(this byte[] buffer, int size)
        {
            return Encoding.ASCII.GetString(buffer).Substring(0, size);
        }
    }
}
