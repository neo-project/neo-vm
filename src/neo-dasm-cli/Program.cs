using System;

namespace neo_dasm_cli
{
    class Program
    {
        static void ShowHelp()
        {
            Console.WriteLine("NEO DASM tool");
            Console.WriteLine("use NEO-DASM-CLI [avm filename]");
            Console.WriteLine("examples:");
            Console.WriteLine("    NEO-DASM-CLI test.avm");
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }
            var inputfile = args[0];
            if (System.IO.File.Exists(inputfile) == false)
            {
                ShowHelp();
                return;
            }
            //var testcode = "515265040066620C0002B8220522616161619366";
            Console.WriteLine("OpenFile:" + inputfile);
            byte[] data = null;
            try
            {
                data = System.IO.File.ReadAllBytes(inputfile);
                if (data.Length > 0xffff)
                    throw new Exception("file is too big.");
            }
            catch (Exception err)
            {
                Console.WriteLine("OpenFile Error:" + err.Message);
                return;
            }
            Neo.ASML.Node.ASMProject proj = null;
            try
            {
                proj = Neo.ASML.DASM.Parse(data);

            }
            catch (Exception err)
            {
                Console.WriteLine("DASM Code Error:" + err.Message);
                return;
            }
            //proj.Dump((str) => Console.WriteLine(str));

            string srccode = null;
            try
            {
                srccode = Neo.ASML.DASM.GenSource(proj);
            }
            catch (Exception err)
            {
                Console.WriteLine("gen SourceCode Error:" + err.Message);
                return;
            }

            var filename = System.IO.Path.GetFileNameWithoutExtension(inputfile);
            var path = System.IO.Path.GetDirectoryName(inputfile);
            var outputfile = System.IO.Path.Combine(path, filename + ".asml.txt");
            try
            {
                System.IO.File.Delete(outputfile);
                System.IO.File.WriteAllText(outputfile, srccode);
                Console.WriteLine("write sourcecode succ:" + outputfile);
            }
            catch (Exception err)
            {
                Console.WriteLine("write sourcecode Error:" + err.Message);
                return;
            }
        }
    }
}
