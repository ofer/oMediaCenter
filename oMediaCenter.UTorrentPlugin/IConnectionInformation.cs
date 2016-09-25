namespace oMediaCenter.UTorrentPlugin
{
    public interface IConnectionInformation
    {
        string IP { get; }
        int Port { get; }
        string Login { get; }
        string Password { get; }
    }
}