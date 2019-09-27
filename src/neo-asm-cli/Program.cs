using System;
using System.IO;

namespace neo_asm_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;
            if (!File.Exists(args[0])) return;
            var text = File.ReadAllText(args[0]);
            //try

            {
                //1.CreateSourceCode
                var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", text);
                code.DumpWords((str) => Console.WriteLine(str));
                //2.ParseSourceCode
                var proj = Neo.ASML.Parser.Parser.Parse(code);
                proj.Dump((str) => Console.WriteLine(str));


                //3 -> avm bytes
                var module = Neo.ASML.Linker.Linker.CreateModule(proj);
                module.Dump((str) => Console.WriteLine(str));
                var bytes = Neo.ASML.Linker.Linker.Link(module);
                //dump bytes
                var hexstr = "";
                foreach (var b in bytes)
                {
                    hexstr += b.ToString("X02");
                }
                Console.WriteLine("hexstr=" + hexstr);

            }
            //catch(Exception err)
            //{
            //    Console.WriteLine("<EERR>" + err.Message);
            //}
            Console.ReadLine();

            //var semantemes = Semanteme.ProcessLines(lines);
            //var table = new AddressTable(semantemes);
            //var script = table.ToScript();
            //string out_path = args.Length >= 2 ? args[1] : Path.ChangeExtension(args[0], "avm");
            //File.WriteAllBytes(out_path, script);
        }
    }
}
