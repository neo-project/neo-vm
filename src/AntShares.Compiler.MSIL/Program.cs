using System.IO;

namespace AntShares.Compiler.MSIL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0) return;
            if (!File.Exists(args[0])) return;
            byte[] script;
            using (FileStream fs = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            {
                script = Converter.Convert(fs);
            }
            string out_path = args.Length >= 2 ? args[1] : Path.ChangeExtension(args[0], "avm");
            File.WriteAllBytes(out_path, script);
        }
    }
}
