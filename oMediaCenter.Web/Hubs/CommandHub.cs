using Microsoft.AspNetCore.SignalR;
using oMediaCenter.Web.Model;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Hubs
{
  public class CommandHub : Hub<ICommandClient>
  {
    private ClientConnectionDictionary _clientConnectionDictionary;

    public CommandHub(ClientConnectionDictionary clientConnectionDictionary)
    {
      _clientConnectionDictionary = clientConnectionDictionary;
    }

    public async Task SendCommand(string targetConnectionId, ClientCommand clientCommand)
    {
      await Clients.Client(_clientConnectionDictionary[targetConnectionId]).CommandReceived(clientCommand);
    }

    public async Task RegisterClient(string clientId)
    {
      if (string.IsNullOrWhiteSpace(clientId))
      {
        /// generate a guid id and send it back
        clientId = Guid.NewGuid().ToString();
        await Clients.Caller.ClientIdGenerated(clientId);
      }


      _clientConnectionDictionary.AddOrUpdate(clientId, Context.ConnectionId, (k, v) => Context.ConnectionId);


      //ClientCommand command = s_commandDictionary.GetOrAdd(clientId, fac => new ClientInfo(DateTime.UtcNow)).LatestCommand;
      //command.Command = "none";
      //command.Parameter = null;
      //command.Date = DateTime.FromFileTimeUtc(0);
      //return clientId;

      //Context.ConnectionId
    }
  }
}
