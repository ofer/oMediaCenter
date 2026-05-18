using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.MetaDatabase
{
	public interface ITitleResolutionNotifier
	{
		Task MediaListUpdated(CancellationToken cancellationToken);
	}
}
