namespace AntShares.Compiler
{
    public class Param
    {
        public Param(string name, string type)
        {
            this.name = name;
            this.type = type;
        }
        public string name
        {
            get;
            private set;
        }
        public string type
        {
            get;
            private set;
        }
        public override string ToString()
        {
            return type + " " + name;
        }
    }
}