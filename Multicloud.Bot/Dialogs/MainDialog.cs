// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Multicloud.Bot.Clu;
using Multicloud.Interfaces;

namespace Multicloud.Bot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger Logger;
        private readonly MulticloudRecognizer _cluRecognizer;
        private readonly IUtilityService _utilityService;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(MulticloudRecognizer cluRecognizer, ILogger<MainDialog> logger, IUtilityService utilityService)
            : base(nameof(MainDialog))
        {
            _cluRecognizer = cluRecognizer;
            Logger = logger;
            _utilityService = utilityService;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: CLU is not configured. To enable all capabilities, add 'CluProjectName', 'CluDeploymentName', 'CluAPIKey' and 'CluAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What can I help you with today?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call CLU and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var cluResult = await _cluRecognizer.RecognizeAsync<Clu.Multicloud>(stepContext.Context, cancellationToken);

            if (cluResult.GetTopIntent().score > 0.9)
            {
				switch (cluResult.GetTopIntent().intent)
				{
					case Clu.Multicloud.Intent.Greeting:
						await _utilityService.SendWelcomeCardAsync(stepContext, cancellationToken);
						break;

					default:
						// Catch all for unhandled intents
						await _utilityService.GetCustomQAResponseAsync(stepContext, cancellationToken);
						break;
				}
			}
            else
                await _utilityService.GetCustomQAResponseAsync(stepContext, cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
