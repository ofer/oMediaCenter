using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using oMediaCenter.Interfaces;
using oMediaCenter.Web.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace oMediaCenter.AlexaSkill.Web.Controllers
{
	[Route("api/alexa")]
	[ApiController]
	public class AlexaClientController : ControllerBase
	{
		private ILogger<AlexaClientController> _logger;
		private IHttpClientFactory _httpClientFactory;

		public AlexaClientController(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
		{
			_logger = loggerFactory.CreateLogger<AlexaClientController>();
			_httpClientFactory = httpClientFactory;
		}

		[HttpPost, Route("movies")]
		public async Task<SkillResponse> PlayMovie(SkillRequest alexaRequest)
		{
			string clientPlayerName = "livingroom";
			//string clientPlayerName = "oferlaptop-mozilla";

			bool endSession = false;

			// verify this is for our application      if (alexaRequest.Session.Application.ApplicationId)


			// verify the request is timed right
			IOutputSpeech innerResponse = null;

			_logger.LogInformation($"Intent Requested {alexaRequest.GetRequestType().Name}");

			if (alexaRequest.GetRequestType() == typeof(LaunchRequest))
			{
				// default launch request, let's just let them know what you can do
				_logger.LogInformation($"Default LaunchRequest made");
				innerResponse = new PlainTextOutputSpeech();
				(innerResponse as PlainTextOutputSpeech).Text = "Welcome to the movie controller.  You can ask us play movies!";
			}
			else if (alexaRequest.GetRequestType() == typeof(IntentRequest))
			{
				var intent = (IntentRequest)alexaRequest.Request;

				innerResponse = await ProcessPlayRequest(clientPlayerName, intent);
			}
			else if (alexaRequest.GetRequestType() == typeof(SessionEndedRequest))
			{
				endSession = true;
			}

			SkillResponse response = new SkillResponse();
			response.Response = new ResponseBody()
			{
				ShouldEndSession = endSession,
				OutputSpeech = innerResponse
			};
			response.Version = "1.0";

			return response;
		}

		private async Task<IOutputSpeech> ProcessPlayRequest(string clientPlayerName, IntentRequest intent)
		{
			var mediaString = await _httpClientFactory.CreateClient().GetStringAsync("http://192.168.1.100:6543/api/v1/media");

			var mediaRecords = JsonConvert.DeserializeObject<List<MediaFileRecord>>(mediaString);

			IOutputSpeech innerResponse;
			// intent request, process the intent
			_logger.LogInformation($"Intent Requested {intent.Intent.Name}");

			var movieName = intent.Intent.Slots["movie"].Value;

			var record = FindMatchingRecord(movieName, mediaRecords);

			if (record != null)
			{
				innerResponse = new PlainTextOutputSpeech();
				try
				{
					_logger.LogInformation($"Play {record.Name}:{record.Hash} on {clientPlayerName}");
					await _httpClientFactory.CreateClient().PutAsync($"http://192.168.1.100:6543/api/v1/client/{clientPlayerName}", JsonContent.Create(
																						new ClientCommand()
																						{
																							Command = "play",
																							Parameter = record.Hash
																						}
						));
					// new System.Net.Http.Formatting.JsonMediaTypeFormatter());
					(innerResponse as PlainTextOutputSpeech).Text = $"Told {clientPlayerName} to play {movieName}.";
				}
				catch (Exception e)
				{
					_logger.LogWarning(e, "Failed to play");
					(innerResponse as PlainTextOutputSpeech).Text = $"Failed to play {movieName} on {clientPlayerName}.";
				}
			}
			else
			{
				innerResponse = new PlainTextOutputSpeech();
				(innerResponse as PlainTextOutputSpeech).Text = $"{movieName} was not found.";
			}

			return innerResponse;
		}

		private MediaFileRecord FindMatchingRecord(string movieName, List<MediaFileRecord> mediaRecords)
		{
			return mediaRecords.FirstOrDefault(mr => MakeSearchableString(mr.Name).Contains(movieName, StringComparison.InvariantCultureIgnoreCase));
		}

		private string MakeSearchableString(string name)
		{
			return name.Replace('.', ' ')
				.Replace('-', ' ')
				.Replace('_', ' ');
		}
	}

}