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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

        public async Task GetChatGPTResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
			string message = await GetGPTResponse(stepContext.Context.Activity.Text);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
        }

        private async Task<string> GetGPTResponse(string text)
        {
            // call an api with a POST request and json body with headers
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_configuration["OpenAI:APIEndpoint"]);
            client.DefaultRequestHeaders.Accept.Clear();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration["OpenAI:APIKey"]);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);

            request.Content = new StringContent($"{{\"model\": \"{_configuration["OpenAI:Model"]}\",\"messages\": [{{\"role\": \"system\",\"content\": \"You are an Q&A assistant.\\n- Follow the questions asked carefully\\n- You must only answer the questions if they fall under the category - {_configuration["OpenAI:QnACategory"]}.\\n- If the question asked does not fall under this category, respond with - \\\"I do not have answer to this question. Please rephrase\\\".\\n- If the questions asked fall under the above category, answer them.\\n- You must never answer any questions if they do not fall under the category. For these questions, above must be the response.\"}},{{\"role\": \"user\", \"content\": \"{text}\"}}]}}", Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request).ConfigureAwait(false);
            var responseString = string.Empty;
            try
            {
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString);
                return responseJson["choices"][0]["message"]["content"].ToString();
            }
            catch (HttpRequestException ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return null;
            }
        }
    }
}
