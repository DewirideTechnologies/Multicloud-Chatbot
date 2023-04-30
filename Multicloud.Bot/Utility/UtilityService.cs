using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Multicloud.Interfaces;
using Multicloud.Services.Cards;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Multicloud.Bot.Utility
{
	public class UtilityService : IUtilityService
	{
		private readonly IConfiguration _configuration;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ILogger<UtilityService> _logger;

		public UtilityService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<UtilityService> logger)
        {
            _configuration = configuration;
			_httpClientFactory = httpClientFactory;
			_logger = logger;
        }

		public async Task GetCustomQAResponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			using var httpClient = _httpClientFactory.CreateClient();

			var customQuestionAnswering = CreateCustomQuestionAnsweringClient(httpClient);

			// Call Custom Question Answering service to get a response.
			_logger.LogInformation("Calling Custom Question Answering");
			var options = new QnAMakerOptions { Top = 1, EnablePreciseAnswer = bool.Parse(_configuration["CustomQnA:EnablePreciseAnswer"]) };
			var response = await customQuestionAnswering.GetAnswersAsync(stepContext.Context, options);

			if (response.Length > 0)
			{
				var activities = new List<IActivity>();

				// Create answer activity.
				var answerText = response[0].Answer;
				var answer = MessageFactory.Text(answerText, answerText);

				// Answer span text has precise answer.
				var preciseAnswerText = response[0].AnswerSpan?.Text;
				if (string.IsNullOrEmpty(preciseAnswerText))
				{
					activities.Add(answer);
				}
				else
				{
					// Create precise answer activity.
					var preciseAnswer = MessageFactory.Text(preciseAnswerText, preciseAnswerText);
					activities.Add(preciseAnswer);

					if (!bool.Parse(_configuration["CustomQnA:DisplayPreciseAnswerOnly"]))
					{
						// Add answer to the reply when it is configured.
						activities.Add(answer);
					}
				}

				await stepContext.Context.SendActivitiesAsync(activities.ToArray(), cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text("No answers were found.", "No answers were found."), cancellationToken);
			}
		}

		private CustomQuestionAnswering CreateCustomQuestionAnsweringClient(HttpClient httpClient)
		{
			// Create a new Custom Question Answering instance initialized with QnAMakerEndpoint.
			return new CustomQuestionAnswering(new QnAMakerEndpoint
			{
				KnowledgeBaseId = _configuration["CustomQnA:ProjectName"],
				EndpointKey = _configuration["CustomQnA:LanguageEndpointKey"],
				Host = _configuration["CustomQnA:LanguageEndpointHostName"],
				QnAServiceType = ServiceType.Language
			},
		   null,
		   httpClient);
		}

		public async Task SendWelcomeCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// get the teams user details
			TeamsChannelAccount teamsUser = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id, cancellationToken);
			var paths = new[] { ".", "Templates", "Common", "WelcomeCard.json" };

			object dataJson = new
			{
				LogoUrl = _configuration["HostName"] + @"/images/logo/logo.png",
				Username = teamsUser.Name
			};

			var welcomeCard = CardsService.CreateAdaptiveCardAttachment(paths, dataJson);
			var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Multicloud!");
			await stepContext.Context.SendActivityAsync(response, cancellationToken);
		}

		public async Task SendWelcomeCardAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
		{
			// get the teams user details
			TeamsChannelAccount teamsUser = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
			var paths = new[] { ".", "Templates", "Common", "WelcomeCard.json" };

			object dataJson = new
			{
				LogoUrl = _configuration["HostName"] + @"/images/logo/logo.png",
				Username = teamsUser.Name
			};

			var welcomeCard = CardsService.CreateAdaptiveCardAttachment(paths, dataJson);
			var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Multicloud!");
			await turnContext.SendActivityAsync(response, cancellationToken);
		}


	}
}
