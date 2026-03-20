using System.Collections.Generic;
using System.Linq;

namespace oMediaCenter.Interfaces
{
  public interface IMediaInformationProvider
  {
    MediaInformation GetEpisodeInfoForFilename(string filename);

    IDictionary<string, MediaInformation> GetEpisodeInfoForFilenames(IEnumerable<string> filenames)
    {
      return filenames.ToDictionary(f => f, f => GetEpisodeInfoForFilename(f));
    }
  }
}
