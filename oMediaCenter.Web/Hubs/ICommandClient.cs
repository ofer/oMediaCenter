using oMediaCenter.Web.Model;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Hubs
{
  public interface ICommandClient
  {
    Task CommandReceived(ClientCommand clientCommand);
    Task ClientIdGenerated(string clientId);
  }
}
