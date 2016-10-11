using Microsoft.AspNetCore.Mvc;
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
        static ConcurrentDictionary<string, ClientCommand> s_commandDictionary = new ConcurrentDictionary<string, ClientCommand>();

        [HttpPost]
        [Route("")]
        public string CreateId()
        {
            string clientId = Guid.NewGuid().ToString();
            ClientCommand command = s_commandDictionary.GetOrAdd(clientId, fac => new ClientCommand());
            command.Command = "none";
            command.Parameter = null;
            command.Date = DateTime.FromFileTimeUtc(0);
            return clientId;
        }

        [HttpGet]
        public string[] GetAllClients()
        {
            return s_commandDictionary.Keys.ToArray();
        }

        [HttpGet]
        [Route("{clientId}")]
        public ClientCommand GetLatestCommands(string clientId)
        {
            ClientCommand command = s_commandDictionary.GetOrAdd(clientId, fac => new ClientCommand());
            return command;
        }

        [HttpPut]
        [Route("{targetClientId}")]
        public void SendClientCommand(string targetClientId, [FromBody]ClientCommand clientCommand)
        {
            if (clientCommand != null)
            {
                ClientCommand command = s_commandDictionary.GetOrAdd(targetClientId, fac => new ClientCommand());
                command.Command = clientCommand.Command;
                command.Parameter = clientCommand.Parameter;
                command.Date = DateTime.UtcNow;
            }
        }
    }
}
