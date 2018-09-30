using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using oMediaCenter.Interfaces;
using oMediaCenter.MetaDatabase;
using oMediaCenter.Web.Model;
//using ShowInfo;

namespace oMediaCenter.Web
{
  public class Startup
  {
    public Startup(IHostingEnvironment env)
    {
      var builder = new ConfigurationBuilder()
          .SetBasePath(env.ContentRootPath)
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

      if (env.IsEnvironment("Development"))
      {
        // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
        builder.AddApplicationInsightsSettings(developerMode: true);
      }

      builder.AddEnvironmentVariables();
      Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; }
    public ILoggerFactory LoggerFactory { get; private set; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<MediaCenterContext>();
      services.AddDbContext<MetaDataContext>();

      // Add framework services.
      services.AddApplicationInsightsTelemetry(Configuration);

      services.AddMvc();

      services.AddOptions();
      services.AddSingleton<IFileReaderPluginLoader, SimpleFileReaderPluginLoader>();

      //services.AddTransient<IAliasProvider, AliasProvider>();
      services.AddTransient<IMediaInformationProvider, MediaInformationProvider>();
      services.AddSingleton<IMediaFileStreamer, MediaFileStreamer>();

      services.AddTransient<IMediaFileProber, FfmpegFileProber>();
      services.AddTransient<IMediaFileConverter, FfmpegFileConverter>();

      services.AddSingleton<IConfiguration>(Configuration);

      services.AddSingleton<IFileReaderPluginLoader, SimpleFileReaderPluginLoader>();
      //services.AddSingleton<IFileReaderPluginLoader>(new SimpleFileReaderPluginLoader(Configuration.GetSection("Plugins")));
      //services.AddSingleton<IFileReaderPluginLoader>(new SimpleFileReaderPluginLoader(Configuration.GetSection("Plugins")));
      services.AddSingleton<IFileReader, FileReader>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
      loggerFactory.AddConsole(Configuration.GetSection("Logging"));
      loggerFactory.AddDebug();

      app.UseDefaultFiles();
      app.UseStaticFiles();

      app.UseMvc();
    }
  }
}
