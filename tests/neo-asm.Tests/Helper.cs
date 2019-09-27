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
    }
}
