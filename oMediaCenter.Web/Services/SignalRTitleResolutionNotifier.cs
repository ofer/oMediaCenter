using Microsoft.AspNetCore.SignalR;
using oMediaCenter.MetaDatabase;
using oMediaCenter.Web.Hubs;
using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Services
{
	public sealed class SignalRTitleResolutionNotifier : ITitleResolutionNotifier
	{
		private readonly IHubContext<CommandHub, ICommandClient> _hubContext;

		public SignalRTitleResolutionNotifier(IHubContext<CommandHub, ICommandClient> hubContext)
		{
			_hubContext = hubContext;
		}

		public Task MediaListUpdated(CancellationToken cancellationToken) =>
			_hubContext.Clients.All.MediaListUpdated();
	}
}
