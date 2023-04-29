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
using Multicloud.Interfaces;
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
        private readonly IUtilityService _utilityService;

        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, IAzureTableService azureTableService, IConfiguration configuration, IUtilityService utilityService)
            : base(conversationState, userState, dialog, logger)
        {
            _azureTableService = azureTableService;
            _configuration = configuration;
            _utilityService = utilityService;
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

				    await _utilityService.SendWelcomeCardAsync(turnContext, cancellationToken);
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }
    }
}
