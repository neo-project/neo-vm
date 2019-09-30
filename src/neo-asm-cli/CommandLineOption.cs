using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm_cli
{
    class CommandLineOption
    {
        public List<string> inputfiles;
        public string outputfile;

        public static CommandLineOption Parse(string[] args)
        {
            if (args == null || args.Length == 0)
                throw new Exception("error params");
            CommandLineOption option = new CommandLineOption();
            option.inputfiles = new List<string>();
            option.outputfile = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i][0] != '-') continue;

                if (args[i] == "-i")
                {
                    option.inputfiles.Add(args[i + 1]);
                    i++;
                    continue;
                }
                if (args[i] == "-o")
                {
                    if (option.outputfile != null)
                        throw new Exception("already have outputfile.");
                    option.outputfile = args[i + 1];
                    i++;
                    continue;
                }
            }
            if (option.inputfiles.Count == 0)
                throw new Exception("error param no input files");
            if (option.outputfile == null)
                throw new Exception("error param no output file");

            return option;
        }

        public static void ShowHelp()
        {
            Console.WriteLine("===How to Use===");
            Console.WriteLine("use \"-i [filename]\" for input file");
            Console.WriteLine("if have multiple files, use \"-i file1 -i file2 -i ...\"");
            Console.WriteLine("use \"-o [filename]\" for output file");
            Console.WriteLine("example:");
            Console.WriteLine("neo-asm-cli -i file1.asml -i file2.asml -o outputfile.nef");
        }
    }
}
