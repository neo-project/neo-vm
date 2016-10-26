namespace AntShares.VM
{
    public interface IApiService
    {
        bool Invoke(string method, ScriptEngine engine);
    }
}
