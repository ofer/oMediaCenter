using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using oMediaCenter.Interfaces;
using oMediaCenter.MetaDatabase;
using oMediaCenter.SubtitleProvidier;
using oMediaCenter.Web.Hubs;
using oMediaCenter.Web.Model;
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
builder.Services.AddSingleton<ClientConnectionDictionary>();
builder.Services.AddSingleton<IFileReaderPluginLoader, SimpleFileReaderPluginLoader>();

//builder.Services.AddTransient<IAliasProvider, AliasProvider>();
builder.Services.AddSingleton<ISubtitleProvider, OpenSubtitlesProvider>();
builder.Services.AddTransient<IMediaInformationProvider, MediaInformationProvider>();
builder.Services.AddSingleton<IMediaFileStreamer, MediaFileStreamer>();

builder.Services.AddTransient<IMediaFileProber, FfmpegFileProber>();
builder.Services.AddTransient<IMediaFileConverter, FfmpegFileConverter>();

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

