namespace oMediaCenter.Interfaces
{
  public class MediaFileProbeInformation
  {
    public string AudioCodec { get; set; }
    public string VideoCodec { get; set; }
    public int NumberOfAudioChannels { get; set; }
    public bool ContainsSubtitles { get; set; }

    /// <summary>Video width in pixels (e.g. 1920).</summary>
    public int Width { get; set; }

    /// <summary>Video height in pixels (e.g. 1080).</summary>
    public int Height { get; set; }

    /// <summary>Video stream bitrate in bits per second.</summary>
    public long VideoBitRate { get; set; }

    /// <summary>Audio stream bitrate in bits per second.</summary>
    public long AudioBitRate { get; set; }

    /// <summary>Total duration of the media file in seconds.</summary>
    public double DurationSeconds { get; set; }

    /// <summary>Video codec profile string (e.g. "High", "Main", "Baseline").</summary>
    public string VideoProfile { get; set; }

    /// <summary>Video codec level (e.g. 31 for level 3.1, 41 for level 4.1).</summary>
    public int VideoLevel { get; set; }
  }
}