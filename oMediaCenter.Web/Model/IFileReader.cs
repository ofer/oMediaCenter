using System.Collections.Generic;
using System.Threading.Tasks;
using oMediaCenter.Interfaces;

namespace oMediaCenter.Web.Model
{
	public interface IFileReader
	{
		Task<IEnumerable<IMediaFile>> GetAll();
		IMediaFile GetByHash(string hash);
    IMediaFile GetMetadataByHash(string hash);
  }
}
