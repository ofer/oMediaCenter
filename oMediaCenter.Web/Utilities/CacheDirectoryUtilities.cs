using System.IO;

namespace oMediaCenter.Web.Utilities
{
  public static class CacheDirectoryUtilities
  {
    private static readonly string CACHE_DIR = "wwwroot" + Path.DirectorySeparatorChar +  "cache";
    private static bool _cacheDirectoryExists = false;

    public static string ToCacheDirectoryFile(this string baseFilename, string extension = null)
    {
      if (!_cacheDirectoryExists)
        if (!Directory.Exists(CACHE_DIR))
        {
          Directory.CreateDirectory(CACHE_DIR);
          _cacheDirectoryExists = true;
        }

      if (extension == null)
        return Path.Combine(CACHE_DIR, baseFilename);
      else
        return Path.Combine(CACHE_DIR, baseFilename + extension);
    }
  }
}
