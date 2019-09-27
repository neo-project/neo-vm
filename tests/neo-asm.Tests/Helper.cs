using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests
{
    class Helper
    {
        public static string Hex2Str(byte[] data)
        {

            var strout = "";
            foreach(var b in data)
            {
                strout += b.ToString("X02");
            }
            return strout;
        }
        public static byte[] Str2Hex(string str)
        {
            if (str.IndexOf("0x") == 0)
                str = str.Substring(2);
            var bytes = new byte[str.Length / 2];
            for(var i=0;i<bytes.Length;i++)
            {
                bytes[i]= byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return bytes;
        }
    }
}
