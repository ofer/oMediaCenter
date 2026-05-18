using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oMediaCenter.Interfaces;
using oMediaCenter.MetaDatabase;
using oMediaCenter.SubtitleProvidier;
using oMediaCenter.Web.Hubs;
using oMediaCenter.Web.Model;
using oMediaCenter.Web.Services;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add builder.Services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle


builder.Services.AddDbContext<MediaCenterContext>();

builder.Services.AddDbContextFactory<MetaDataContext>();

//builder.Services.AddDbContext<MetaDataContext>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

builder.Services.AddOptions();

builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TitleResolverCache>(sp =>
{
	var logger = sp.GetRequiredService<ILogger<TitleResolverCache>>();
	var cacheDir = Path.GetDirectoryName(typeof(MetaDataContext).Assembly.Location) ?? ".";
	var cachePath = Path.Combine(cacheDir, "title-resolver-cache.json");
	return new TitleResolverCache(cachePath, logger);
});
builder.Services.AddSingleton<ITitleResolutionNotifier, SignalRTitleResolutionNotifier>();
builder.Services.AddSingleton<BackgroundTitleResolver>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackgroundTitleResolver>());
builder.Services.AddTransient<ILlmTitleResolver>(sp =>
{
	var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
	if (string.IsNullOrEmpty(options.BaseUrl))
		return new NoOpTitleResolver();
	return new OpenAiTitleResolver(
		sp.GetRequiredService<TitleResolverCache>(),
		sp.GetRequiredService<BackgroundTitleResolver>());
});

builder.Services.AddSingleton<ClientConnectionDictionary>();
builder.Services.AddSingleton<IFileReaderPluginLoader, SimpleFileReaderPluginLoader>();

//builder.Services.AddTransient<IAliasProvider, AliasProvider>();
builder.Services.AddSingleton<ISubtitleProvider, OpenSubtitlesProvider>();
builder.Services.AddTransient<IMediaInformationProvider, MediaInformationProvider>();
builder.Services.AddSingleton<IMediaFileStreamer, MediaFileStreamer>();

builder.Services.AddTransient<IMediaFileProber, FfmpegFileProber>();
builder.Services.AddTransient<IMediaFileConverter, FfmpegFileConverter>();

builder.Services.AddSingleton<TranscodingJobManager>();
builder.Services.AddSingleton<ITranscodingJobManager>(sp => sp.GetRequiredService<TranscodingJobManager>());
builder.Services.AddSingleton<HlsPlaylistGenerator>();

//builder.Services.AddSingleton<IConfiguration>(Configuration);

builder.Services.AddSingleton<IFileReaderPluginLoader, SimpleFileReaderPluginLoader>();
builder.Services.AddSingleton<IFileReader, FileReader>();



var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
  ServeUnknownFileTypes = true,
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  //app.UseSwagger();
  //app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHub<CommandHub>("/commandHub");
app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
