using System;
using System.Security.Cryptography;
using System.Text;

namespace Memphis.Client.Helper
{
    internal class MemphisUtil
    {
        internal static string GetInternalName(string name)
        {
            return name.Replace(".", "#");
        }
        
        internal static string GetStationName(string internalStationName)
        {
            return internalStationName.Replace("#", ".");
        }
        
        internal static readonly char[] chars =
            "0123456789abcdef".ToCharArray(); 

        internal static string GetUniqueKey(int size)
        {
            byte[] data = new byte[4*size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }
    }
}