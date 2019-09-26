using Neo.Compiler.ASM;
using System;
using System.IO;

namespace neo_asm
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
                var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", text);
                Console.WriteLine("==scan finish words=" + code.words.Count);
                foreach (var w in code.words)
                {
                    Console.WriteLine(w.ToString());
                }
                var doc = Neo.ASML.Parser.DocumentParser.Parse(code);
                doc.Dump((str) => Console.WriteLine(str));
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
