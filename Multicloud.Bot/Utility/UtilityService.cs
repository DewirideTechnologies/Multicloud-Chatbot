using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Multicloud.Interfaces;
using Multicloud.Services.Cards;
using System.Threading;
using System.Threading.Tasks;

namespace Multicloud.Bot.Utility
{
	public class UtilityService : IUtilityService
	{
		private readonly IConfiguration _configuration;

        public UtilityService(IConfiguration configuration)
        {
            _configuration = configuration;
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
