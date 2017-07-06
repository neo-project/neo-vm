using AntShares.Compiler.JVM;
using System;
using System.Reflection;

namespace AntShares.Compiler
{
    public class Program
    {
        //Console.WriteLine("helo ha:"+args[0]); //普通输出
        //Console.WriteLine("<WARN> 这是一个严重的问题。");//警告输出，黄字
        //Console.WriteLine("<WARN|aaaa.cs(1)> 这是ee一个严重的问题。");//警告输出，带文件名行号
        //Console.WriteLine("<ERR> 这是一个严重的问题。");//错误输出，红字
        //Console.WriteLine("<ERR|aaaa.cs> 这是ee一个严重的问题。");//错误输出，带文件名
        //Console.WriteLine("SUCC");//输出这个表示编译成功
        //控制台输出约定了特别的语法
        public static void Main(string[] args)
        {

            //set console
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var log = new DefLogger();
            log.Log("AntShars.Compiler.JVM console app v" + Assembly.GetEntryAssembly().GetName().Version);
            if (args.Length == 0)
            {
                log.Log("need one param for .class filename.");
                return;
            }
            string filename = args[0];
            string onlyname = System.IO.Path.GetFileNameWithoutExtension(filename);
            //javaloader.ClassFile classFile = null;
            JavaModule module = new JavaModule();


            byte[] bytes = null;
            bool bSucc = false;
            //convert and build
            try
            {
                var path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                path = System.IO.Path.GetDirectoryName(path);
                module.LoadJar(System.IO.Path.Combine(path, "AntShares.SmartContract.Framework.jar"));
                module.LoadClass(filename);
                var conv = new ModuleConverter(log);

                AntsModule am = conv.Convert(module);
                bytes = am.Build();
                log.Log("convert succ");
            }
            catch (Exception err)
            {
                log.Log("Convert Error:" + err.ToString());
                return;
            }
            //write bytes
            try
            {

                string bytesname = onlyname + ".avm";

                System.IO.File.Delete(bytesname);
                System.IO.File.WriteAllBytes(bytesname, bytes);
                log.Log("write:" + bytesname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write Bytes Error:" + err.ToString());
                return;
            }


            if (bSucc)
            {
                log.Log("SUCC");
            }
        }
    }
}
