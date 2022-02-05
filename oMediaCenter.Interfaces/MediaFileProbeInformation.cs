namespace oMediaCenter.Interfaces
{
  public class MediaFileProbeInformation
  {
    public string AudioCodec { get; set; }
    public string VideoCodec { get; set; }
    public int NumberOfAudioChannels { get; set; }
    public bool ContainsSubtitles { get; set; }
  }
}