using oMediaCenter.Interfaces;
using oMediaCenter.Web.Services;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace oMediaCenter.Tests
{
  public class HlsPlaylistGeneratorTests
  {
    private readonly HlsPlaylistGenerator _generator;

    public HlsPlaylistGeneratorTests()
    {
      _generator = new HlsPlaylistGenerator();
    }

    private MediaFileProbeInformation CreateProbeResult(
      string videoCodec = "h264",
      string audioCodec = "aac",
      int width = 1920,
      int height = 1080,
      long videoBitRate = 5_000_000,
      long audioBitRate = 128_000,
      double durationSeconds = 120.0,
      string videoProfile = "High",
      int videoLevel = 41)
    {
      return new MediaFileProbeInformation
      {
        VideoCodec = videoCodec,
        AudioCodec = audioCodec,
        Width = width,
        Height = height,
        VideoBitRate = videoBitRate,
        AudioBitRate = audioBitRate,
        DurationSeconds = durationSeconds,
        VideoProfile = videoProfile,
        VideoLevel = videoLevel,
        NumberOfAudioChannels = 2,
        ContainsSubtitles = false
      };
    }

    // --- Master Playlist Tests ---

    [Fact]
    public void GenerateMasterPlaylist_ContainsCorrectCodecString()
    {
      // H.264 High Profile Level 4.1 + AAC-LC
      var probe = CreateProbeResult(videoCodec: "h264", audioCodec: "aac", videoProfile: "High", videoLevel: 41);

      string playlist = _generator.GenerateMasterPlaylist(probe, "testhash");

      // High profile = 0x64 = 100, level 41 = 0x29 = 41
      Assert.Contains("CODECS=\"avc1.640029,mp4a.40.2\"", playlist);
    }

    [Fact]
    public void GenerateMasterPlaylist_ContainsCorrectResolution()
    {
      var probe = CreateProbeResult(width: 1920, height: 1080);

      string playlist = _generator.GenerateMasterPlaylist(probe, "testhash");

      Assert.Contains("RESOLUTION=1920x1080", playlist);
    }

    [Fact]
    public void GenerateMasterPlaylist_ContainsCorrectBandwidth()
    {
      var probe = CreateProbeResult(videoBitRate: 5_000_000, audioBitRate: 128_000);

      string playlist = _generator.GenerateMasterPlaylist(probe, "testhash");

      Assert.Contains("BANDWIDTH=5128000", playlist);
    }

    [Fact]
    public void GenerateMasterPlaylist_ReferencesVariantPlaylistUrl()
    {
      var probe = CreateProbeResult();

      string playlist = _generator.GenerateMasterPlaylist(probe, "abc123");

      Assert.Contains("/api/v1/media/abc123/hls/variant.m3u8", playlist);
    }

    [Fact]
    public void GenerateMasterPlaylist_WithSubtitles_AdvertisesSubtitleRendition()
    {
      var probe = CreateProbeResult();

      string playlist = _generator.GenerateMasterPlaylist(probe, "abc123", hasSubtitles: true);

      Assert.Contains("#EXT-X-MEDIA:TYPE=SUBTITLES", playlist);
      Assert.Contains("SUBTITLES=\"subs\"", playlist);
      Assert.Contains("/api/v1/media/abc123/hls/subtitles.m3u8", playlist);
    }

    [Fact]
    public void GenerateSubtitlePlaylist_ReferencesSubtitleEndpoint()
    {
      var probe = CreateProbeResult(durationSeconds: 120.0);

      string playlist = _generator.GenerateSubtitlePlaylist(probe, "abc123");

      Assert.Contains("#EXT-X-PLAYLIST-TYPE:VOD", playlist);
      Assert.Contains("#EXT-X-TARGETDURATION:120", playlist);
      Assert.Contains("/api/v1/media/abc123/subtitles", playlist);
    }

    // --- Variant Playlist Tests ---

    [Fact]
    public void GenerateVariantPlaylist_HasCorrectSegmentCount()
    {
      // 120s file with 6s segments = exactly 20 segments
      var probe = CreateProbeResult(durationSeconds: 120.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 6);

      int extinfCount = playlist.Split('\n').Count(line => line.StartsWith("#EXTINF:"));
      Assert.Equal(20, extinfCount);
    }

    [Fact]
    public void GenerateVariantPlaylist_LastSegmentHandlesRemainder()
    {
      // 125s file with 6s segments = 20 full segments (120s) + 1 remainder segment (5s) = 21 total
      var probe = CreateProbeResult(durationSeconds: 125.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 6);

      string[] lines = playlist.Split('\n');
      int extinfCount = lines.Count(line => line.StartsWith("#EXTINF:"));
      Assert.Equal(21, extinfCount);

      // Find the last #EXTINF line and verify it's ~5 seconds
      string lastExtinf = lines.Last(line => line.StartsWith("#EXTINF:"));
      // Format: #EXTINF:5.000000,
      string durationStr = lastExtinf.Replace("#EXTINF:", "").TrimEnd(',');
      double lastDuration = double.Parse(durationStr, CultureInfo.InvariantCulture);
      Assert.Equal(5.0, lastDuration, precision: 3);
    }

    [Fact]
    public void GenerateVariantPlaylist_SegmentUrlsContainStartAndDurationTicks()
    {
      var probe = CreateProbeResult(durationSeconds: 18.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 6);

      string[] lines = playlist.Split('\n');
      var segmentUrls = lines.Where(line => line.Contains("/hls/") && line.EndsWith(".ts?startTicks=" + lines.ToString()) == false && line.Contains(".ts?")).ToArray();

      // Should be 3 segments: 0-6s, 6-12s, 12-18s
      // Actually, let me just pick out the segment URL lines properly
      segmentUrls = lines.Where(line => line.Contains(".ts?startTicks=")).ToArray();
      Assert.Equal(3, segmentUrls.Length);

      // Verify first segment: startTicks=0, durationTicks=6s worth of ticks
      long expectedDurationTicks = 6 * TimeSpan.TicksPerSecond;
      Assert.Contains("startTicks=0", segmentUrls[0]);
      Assert.Contains($"durationTicks={expectedDurationTicks}", segmentUrls[0]);

      // Verify second segment: startTicks = 6s worth of ticks
      long expectedStartTicks1 = 6 * TimeSpan.TicksPerSecond;
      Assert.Contains($"startTicks={expectedStartTicks1}", segmentUrls[1]);
      Assert.Contains($"durationTicks={expectedDurationTicks}", segmentUrls[1]);

      // Verify third segment: startTicks = 12s worth of ticks
      long expectedStartTicks2 = 12 * TimeSpan.TicksPerSecond;
      Assert.Contains($"startTicks={expectedStartTicks2}", segmentUrls[2]);
      Assert.Contains($"durationTicks={expectedDurationTicks}", segmentUrls[2]);
    }

    [Fact]
    public void GenerateVariantPlaylist_IsValidVodPlaylist()
    {
      var probe = CreateProbeResult(durationSeconds: 60.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 6);

      Assert.Contains("#EXTM3U", playlist);
      Assert.Contains("#EXT-X-PLAYLIST-TYPE:VOD", playlist);
      Assert.Contains("#EXT-X-ENDLIST", playlist);
      Assert.Contains("#EXT-X-TARGETDURATION:6", playlist);
      Assert.Contains("#EXT-X-MEDIA-SEQUENCE:0", playlist);
    }

    [Fact]
    public void GenerateVariantPlaylist_ZeroDuration_ReturnsEmptyPlaylist()
    {
      var probe = CreateProbeResult(durationSeconds: 0.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 6);

      Assert.Contains("#EXTM3U", playlist);
      Assert.Contains("#EXT-X-PLAYLIST-TYPE:VOD", playlist);
      Assert.Contains("#EXT-X-ENDLIST", playlist);

      // Should have no #EXTINF entries
      int extinfCount = playlist.Split('\n').Count(line => line.StartsWith("#EXTINF:"));
      Assert.Equal(0, extinfCount);
    }

    // --- Codec String Tests ---

    [Fact]
    public void BuildCodecString_H264High41_ReturnsCorrectString()
    {
      var probe = CreateProbeResult(videoCodec: "h264", audioCodec: "aac", videoProfile: "High", videoLevel: 41);

      string codecs = HlsPlaylistGenerator.BuildCodecString(probe);

      // High = 0x64, level 41 = 0x29
      Assert.Equal("avc1.640029,mp4a.40.2", codecs);
    }

    [Fact]
    public void BuildCodecString_H264Baseline30_ReturnsCorrectString()
    {
      var probe = CreateProbeResult(videoCodec: "h264", audioCodec: "aac", videoProfile: "Baseline", videoLevel: 30);

      string codecs = HlsPlaylistGenerator.BuildCodecString(probe);

      // Baseline = 0x42, level 30 = 0x1E
      Assert.Equal("avc1.42001E,mp4a.40.2", codecs);
    }

    [Fact]
    public void BuildCodecString_H264Main31_ReturnsCorrectString()
    {
      var probe = CreateProbeResult(videoCodec: "h264", audioCodec: "aac", videoProfile: "Main", videoLevel: 31);

      string codecs = HlsPlaylistGenerator.BuildCodecString(probe);

      // Main = 0x4D, level 31 = 0x1F
      Assert.Equal("avc1.4D001F,mp4a.40.2", codecs);
    }

    [Fact]
    public void GetAudioCodecString_Opus_ReturnsOpus()
    {
      var probe = CreateProbeResult(audioCodec: "opus");

      string audioCodec = HlsPlaylistGenerator.GetAudioCodecString(probe);

      Assert.Equal("Opus", audioCodec);
    }

    [Fact]
    public void GenerateMasterPlaylist_HevcCodec_ReportsTranscodedH264()
    {
      // HEVC sources are transcoded to H.264 by ffmpeg, so the master playlist
      // must advertise the output codec (avc1) not the source codec (hev1).
      var probe = CreateProbeResult(videoCodec: "hevc", audioCodec: "aac");

      string playlist = _generator.GenerateMasterPlaylist(probe, "testhash");

      Assert.Contains("avc1.640028", playlist);
      Assert.DoesNotContain("hev1", playlist);
    }

    [Fact]
    public void GenerateMasterPlaylist_MissingBitrate_UsesFallback()
    {
      var probe = CreateProbeResult(videoBitRate: 0, audioBitRate: 0);

      string playlist = _generator.GenerateMasterPlaylist(probe, "testhash");

      // Should use the 2Mbps fallback
      Assert.Contains("BANDWIDTH=2000000", playlist);
    }

    [Fact]
    public void GenerateVariantPlaylist_SegmentUrlsPointToCorrectEndpoint()
    {
      var probe = CreateProbeResult(durationSeconds: 12.0);

      string playlist = _generator.GenerateVariantPlaylist(probe, "myhash", segmentLength: 6);

      Assert.Contains("/api/v1/media/myhash/hls/0.ts?", playlist);
      Assert.Contains("/api/v1/media/myhash/hls/1.ts?", playlist);
    }

    [Fact]
    public void GenerateVariantPlaylist_UsesDefaultSegmentLength()
    {
      _generator.DefaultSegmentLength = 6;
      var probe = CreateProbeResult(durationSeconds: 12.0);

      // Pass 0 to use default
      string playlist = _generator.GenerateVariantPlaylist(probe, "testhash", segmentLength: 0);

      Assert.Contains("#EXT-X-TARGETDURATION:6", playlist);
      int extinfCount = playlist.Split('\n').Count(line => line.StartsWith("#EXTINF:"));
      Assert.Equal(2, extinfCount);
    }
  }
}
