using System;
using System.Collections.Generic;
using System.IO;

namespace neo_asm_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            //parse commandline
            CommandLineOption option = null;
            try
            {
                option = CommandLineOption.Parse(args);
            }
            catch
            {
                CommandLineOption.ShowHelp();
                return;
            }

            List<Neo.ASML.Node.SourceCode> codes = new List<Neo.ASML.Node.SourceCode>();
            //step01.check srcfiles
            foreach (var file in option.inputfiles)
            {
                Console.WriteLine("ParseFile:" + file);
                if (System.IO.File.Exists(file) == false)
                {
                    Console.WriteLine("can not find file:" + file);
                    return;
                }
                try
                {
                    var text = System.IO.File.ReadAllText(file);

                    var code = Neo.ASML.Parser.WordScanner.CreateSourceCode(file, text);
                    codes.Add(code);

                }
                catch (Exception err)
                {
                    Console.WriteLine("<ERROR>ParseCode:" + file + "," + err.Message);
                    return;
                }
                //Console.WriteLine("<DONE>ParseFile");
            }

            //step02.ParseProj
            Neo.ASML.Node.ASMProject proj = null;
            //parse proj
            try
            {
                proj = Neo.ASML.Parser.Parser.Parse(codes.ToArray());
                //Console.WriteLine("<DONE>GenProj");
            }
            catch (Exception err)
            {
                Console.WriteLine("<ERROR>GenProj:" + err.Message);
                return;
            }

            //step03.CreateModule
            Neo.ASML.Linker.BuildedModule module = null;
            try
            {
                module = Neo.ASML.Linker.Linker.CreateModule(proj);
                //Console.WriteLine("<DONE>BuildModule");
            }
            catch (Exception err)
            {
                Console.WriteLine("<ERROR>BuildModule:" + err.Message);
                return;
            }

            //step04.Link
            byte[] avm;
            try
            {
                avm = Neo.ASML.Linker.Linker.Link(module);
                //Console.WriteLine("<DONE>GenAVM");
            }
            catch (Exception err)
            {
                Console.WriteLine("<ERROR>GenAVM:" + err.Message);
                return;
            }
            Console.WriteLine("GenAVM length=" + avm.Length);

            Console.WriteLine("OutputFile:" + option.outputfile);
            try
            {
                System.IO.File.Delete(option.outputfile);
                System.IO.File.WriteAllBytes(option.outputfile, avm);
            }
            catch (Exception err)
            {
                Console.WriteLine("<ERROR>OutputFile:" + option.outputfile + "," + err.Message);
                return;
            }


            var outputdebuginfo = System.IO.Path.GetFileName(option.outputfile) + ".avmlinfo.json";
            Console.WriteLine("OutputDebugInfoFile:" + outputdebuginfo);
            try
            {
                System.IO.File.Delete(outputdebuginfo);
                var json = module.genDebugInfo();
                var text = json.ToString(Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(outputdebuginfo, text);
            }
            catch (Exception err)
            {
                Console.WriteLine("<ERROR>OutputFile:" + outputdebuginfo + "," + err.Message);
                return;
            }
        }

    }
}
