// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Multicloud.Interfaces.AzureStorage;
using Multicloud.Services.Cards;
using Newtonsoft.Json;

namespace Multicloud.Bot.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {
        private readonly IAzureTableService _azureTableService;
        private readonly IConfiguration _configuration;

        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, IAzureTableService azureTableService, IConfiguration configuration)
            : base(conversationState, userState, dialog, logger)
        {
            _azureTableService = azureTableService;
            _configuration = configuration;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // store teams user details in Azure Table Storage
                    await _azureTableService.StoreUserDetailsAsync(turnContext, cancellationToken);

					// get the teams user details
					TeamsChannelAccount teamsUser = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);

					var paths = new[] { ".", "Templates", "Common", "WelcomeCard.json" };

					object dataJson = new
                    {
						LogoUrl = _configuration["HostName"]+@"/images/logo/logo.png",
						Username = teamsUser.Name
					};

					var welcomeCard = CardsService.CreateAdaptiveCardAttachment(paths, dataJson);
                    var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Multicloud!");
                    await turnContext.SendActivityAsync(response, cancellationToken);
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }
    }
}
