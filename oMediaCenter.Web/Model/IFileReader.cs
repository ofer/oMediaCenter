using System.Collections.Generic;
using oMediaCenter.Interfaces;

namespace oMediaCenter.Web.Model
{
	public interface IFileReader
	{
		IEnumerable<IMediaFile> GetAll();
		IMediaFile GetByHash(string hash);
	}
}