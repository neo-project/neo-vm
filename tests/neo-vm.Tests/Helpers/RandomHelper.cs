using System;

namespace Neo.Test.Helpers
{
    public class RandomHelper
    {
        const string _randchars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        static Random _rand = new Random();

        /// <summary>
        /// Get random buffer
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Buffer</returns>
        public static byte[] RandBuffer(int length)
        {
            var buffer = new byte[length];
            _rand.NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Get random string
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Buffer</returns>
        public static string RandString(int length)
        {
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = _randchars[random.Next(_randchars.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Get random short
        /// </summary>
        /// <returns>Int16</returns>
        public static short RandInt16()
        {
            return (short)_rand.Next(short.MaxValue);
        }
    }
}