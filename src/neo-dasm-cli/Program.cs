using System;

namespace neo_dasm_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var testcode = "515265040066620C0002B8220522616161619366";
            byte[] data = new byte[testcode.Length / 2];
            for (var i = 0; i < testcode.Length / 2; i++)
            {
                data[i] = byte.Parse(testcode.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            var proj = Neo.ASML.DASM.Parse(data);
            proj.Dump((str) => Console.WriteLine(str));

            var src = Neo.ASML.DASM.GenSource(proj);
            Console.WriteLine("===srccode===");
            Console.WriteLine(src);
            Console.WriteLine("Hello World!");
        }
    }
}
