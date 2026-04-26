using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using System.Threading;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Services
{
  /// <summary>
  /// Interface for the transcoding job manager, enabling dependency injection and testability.
  /// </summary>
  public interface ITranscodingJobManager
  {
    /// <summary>Returns the active transcoding job for the given media hash, or null.</summary>
    TranscodingJob GetJob(string hash);

    /// <summary>Returns true if there is an active (non-exited) transcoding job for the given hash.</summary>
    bool HasActiveJob(string hash);

    /// <summary>
    /// Returns cached probe results for the given hash, or probes the file and caches the result.
    /// </summary>
    MediaFileProbeInformation GetOrCacheProbeResult(string hash, string sourceFile);

    /// <summary>Starts a new ffmpeg transcoding job, stopping any existing job for this hash first.</summary>
    TranscodingJob StartTranscoding(string hash, string sourceFile, int startSegment, MediaFileProbeInformation probeResult);

    /// <summary>Stops and removes the transcoding job for the given hash.</summary>
    void StopTranscoding(string hash);

    /// <summary>
    /// Waits for a specific segment to become ready (fully written).
    /// Returns the full path to the segment file.
    /// </summary>
    Task<string> GetOrWaitForSegment(string hash, int segmentIndex, CancellationToken cancellationToken = default);
  }
}
