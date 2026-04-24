using oMediaCenter.Interfaces;
using System;
using System.Globalization;
using System.Text;

namespace oMediaCenter.Web.Services
{
  /// <summary>
  /// Generates HLS master and variant playlists in-memory from probe data,
  /// without writing to disk. Follows the Jellyfin pattern of VOD manifests
  /// with on-demand segment generation.
  /// </summary>
  public class HlsPlaylistGenerator
  {
    /// <summary>Default segment length in seconds.</summary>
    public int DefaultSegmentLength { get; set; } = 6;

    /// <summary>
    /// Generates a master HLS playlist (m3u8) for the given media file.
    /// Contains a single variant stream with correct BANDWIDTH, RESOLUTION, and CODECS.
    /// </summary>
    /// <param name="probeResult">Probe information from ffprobe.</param>
    /// <param name="hash">Media file hash used for URL construction.</param>
    /// <returns>The master playlist as a string.</returns>
    public string GenerateMasterPlaylist(MediaFileProbeInformation probeResult, string hash)
    {
      var sb = new StringBuilder();
      sb.AppendLine("#EXTM3U");

      long bandwidth = probeResult.VideoBitRate + probeResult.AudioBitRate;
      // Use a sensible default if bitrate data is missing
      if (bandwidth <= 0)
        bandwidth = 2_000_000;

      string resolution = $"{probeResult.Width}x{probeResult.Height}";
      string codecs = BuildCodecString(probeResult);

      sb.Append("#EXT-X-STREAM-INF:");
      sb.Append($"BANDWIDTH={bandwidth}");

      if (probeResult.Width > 0 && probeResult.Height > 0)
        sb.Append($",RESOLUTION={resolution}");

      if (!string.IsNullOrEmpty(codecs))
        sb.Append($",CODECS=\"{codecs}\"");

      sb.AppendLine();

      // Point to the variant playlist endpoint
      sb.AppendLine($"/api/v1/media/{hash}/hls/variant.m3u8");

      return sb.ToString();
    }

    /// <summary>
    /// Generates a variant HLS playlist (m3u8) with all segments listed upfront as a VOD manifest.
    /// No segments are pre-generated — each segment URL carries startTicks and durationTicks
    /// so the server knows where to seek when that segment is requested.
    /// </summary>
    /// <param name="probeResult">Probe information from ffprobe.</param>
    /// <param name="hash">Media file hash used for URL construction.</param>
    /// <param name="segmentLength">Segment length in seconds (0 = use default).</param>
    /// <returns>The variant playlist as a string.</returns>
    public string GenerateVariantPlaylist(MediaFileProbeInformation probeResult, string hash, int segmentLength = 0)
    {
      if (segmentLength <= 0)
        segmentLength = DefaultSegmentLength;

      double totalDuration = probeResult.DurationSeconds;

      var sb = new StringBuilder();
      sb.AppendLine("#EXTM3U");
      sb.AppendLine("#EXT-X-VERSION:3");
      sb.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
      sb.AppendLine($"#EXT-X-TARGETDURATION:{segmentLength}");
      sb.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

      if (totalDuration <= 0)
      {
        // Edge case: zero or unknown duration — return empty but valid playlist
        sb.AppendLine("#EXT-X-ENDLIST");
        return sb.ToString();
      }

      int segmentIndex = 0;
      double remainingDuration = totalDuration;

      while (remainingDuration > 0)
      {
        double currentSegmentDuration = Math.Min(segmentLength, remainingDuration);
        long startTicks = (long)(segmentIndex * segmentLength * TimeSpan.TicksPerSecond);
        long durationTicks = (long)(currentSegmentDuration * TimeSpan.TicksPerSecond);

        sb.AppendLine($"#EXTINF:{currentSegmentDuration.ToString("F6", CultureInfo.InvariantCulture)},");
        sb.AppendLine($"/api/v1/media/{hash}/hls/{segmentIndex}.ts?startTicks={startTicks}&durationTicks={durationTicks}");

        segmentIndex++;
        remainingDuration -= segmentLength;
      }

      sb.AppendLine("#EXT-X-ENDLIST");

      return sb.ToString();
    }

    /// <summary>
    /// Builds the CODECS string for the HLS master playlist #EXT-X-STREAM-INF line.
    /// Maps video/audio codec names to RFC 6381 codec strings.
    /// </summary>
    public static string BuildCodecString(MediaFileProbeInformation probeResult)
    {
      string videoCodecStr = GetVideoCodecString(probeResult);
      string audioCodecStr = GetAudioCodecString(probeResult);

      if (!string.IsNullOrEmpty(videoCodecStr) && !string.IsNullOrEmpty(audioCodecStr))
        return $"{videoCodecStr},{audioCodecStr}";
      if (!string.IsNullOrEmpty(videoCodecStr))
        return videoCodecStr;
      if (!string.IsNullOrEmpty(audioCodecStr))
        return audioCodecStr;

      return null;
    }

    /// <summary>
    /// Returns the RFC 6381 video codec string (e.g., "avc1.640028" for H.264 High Level 4.0).
    /// </summary>
    public static string GetVideoCodecString(MediaFileProbeInformation probeResult)
    {
      string codec = probeResult.VideoCodec?.ToLowerInvariant();
      if (codec == null)
        return null;

      switch (codec)
      {
        case "h264":
          // H.264 is passed through (copy) — report actual profile/level
          return GetH264CodecString(probeResult.VideoProfile, probeResult.VideoLevel);
        default:
          // Everything else (HEVC, VP9, AV1, etc.) is transcoded to libx264 H.264 High
          return "avc1.640028";
      }
    }

    /// <summary>
    /// Builds an H.264 codec string from profile and level.
    /// Format: avc1.XXYYZZ where XX=profile_idc, YY=constraint_set flags (00), ZZ=level_idc.
    /// </summary>
    public static string GetH264CodecString(string profile, int level)
    {
      // Map profile name to profile_idc
      int profileIdc = 0x64; // default to High (100)
      if (!string.IsNullOrEmpty(profile))
      {
        switch (profile.ToLowerInvariant())
        {
          case "baseline":
          case "constrained baseline":
            profileIdc = 0x42;
            break;
          case "main":
            profileIdc = 0x4D;
            break;
          case "high":
            profileIdc = 0x64;
            break;
          case "high 10":
            profileIdc = 0x6E;
            break;
          case "high 4:2:2":
            profileIdc = 0x7A;
            break;
          case "high 4:4:4 predictive":
            profileIdc = 0xF4;
            break;
        }
      }

      // Level is stored as level * 10 in ffprobe (e.g., 31 = level 3.1, 41 = level 4.1)
      int levelIdc = level > 0 ? level : 40; // default to level 4.0

      return $"avc1.{profileIdc:X2}00{levelIdc:X2}";
    }

    /// <summary>
    /// Returns the RFC 6381 audio codec string (e.g., "mp4a.40.2" for AAC-LC).
    /// </summary>
    public static string GetAudioCodecString(MediaFileProbeInformation probeResult)
    {
      string codec = probeResult.AudioCodec?.ToLowerInvariant();
      if (codec == null)
        return null;

      switch (codec)
      {
        case "aac":
          // AAC is passed through (copy) — report AAC-LC
          return "mp4a.40.2";
        default:
          // Everything else (AC3, EAC3, Opus, FLAC, etc.) is transcoded to AAC
          return "mp4a.40.2";
      }
    }
  }
}
