using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using oMediaCenter.Web.Hubs;
using oMediaCenter.Web.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Controllers
{
  [Route("api/v1/client")]
  public class ClientController : Controller
  {
    private readonly IHubContext<CommandHub, ICommandClient> _hubContext;
    private readonly ClientConnectionDictionary _clientConnectionDictionary;

    public ClientController(ClientConnectionDictionary clientConnectionDictionary, IHubContext<CommandHub, ICommandClient> hubContext)
    {
      _hubContext = hubContext;
      _clientConnectionDictionary = clientConnectionDictionary;
    }

    //[HttpPost]
    //[Route("")]
    //public string CreateId()
    //{
    //  string clientId = Guid.NewGuid().ToString();
    //  ClientCommand command = s_commandDictionary.GetOrAdd(clientId, fac => new ClientInfo(DateTime.UtcNow)).LatestCommand;
    //  command.Command = "none";
    //  command.Parameter = null;
    //  command.Date = DateTime.FromFileTimeUtc(0);
    //  return clientId;
    //}

    [HttpGet]
    public string[] GetAllClients()
    {
      return _clientConnectionDictionary.Select(kvp => kvp.Key).ToArray();
    }

    //[HttpGet]
    //[Route("{clientId}")]
    //public ClientCommand GetLatestCommands(string clientId)
    //{
    //  ClientInfo info = s_commandDictionary.GetOrAdd(clientId, fac => new ClientInfo(DateTime.UtcNow));
    //  info.LastPoll = DateTime.UtcNow;
    //  return info.LatestCommand;
    //}

    //[HttpPut]
    //[Route("{targetClientId}")]
    //public void SendClientCommand(string targetClientId, [FromBody] ClientCommand clientCommand)
    //{
    //  if (clientCommand != null)
    //  {
    //    ClientCommand command = s_commandDictionary.GetOrAdd(targetClientId, fac => new ClientInfo(DateTime.UtcNow)).LatestCommand;
    //    command.Command = clientCommand.Command;
    //    command.Parameter = clientCommand.Parameter;
    //    command.Date = DateTime.UtcNow;
        
    //    _hubContext.Clients.All.CommandReceived(clientCommand);
    //  }
    //}
  }
}
