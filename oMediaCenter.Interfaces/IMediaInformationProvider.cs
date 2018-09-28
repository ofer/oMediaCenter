namespace oMediaCenter.Interfaces
{
  public interface IMediaInformationProvider
  {
    MediaInformation GetEpisodeInfoForFilename(string filename);
  }
}
