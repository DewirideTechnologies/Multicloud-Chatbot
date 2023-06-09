﻿using System;
using Microsoft.Bot.Builder;
using Multicloud.Bot.Clu;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Multicloud.Bot
{
	public class MulticloudRecognizer
	{
        private readonly CluRecognizer _recognizer;

        public MulticloudRecognizer(IConfiguration configuration)
        {
            var cluIsConfigured = !string.IsNullOrEmpty(configuration["Clu:ProjectName"]) && !string.IsNullOrEmpty(configuration["Clu:DeploymentName"]) && !string.IsNullOrEmpty(configuration["Clu:APIKey"]) && !string.IsNullOrEmpty(configuration["Clu:APIHostName"]);
            if (cluIsConfigured)
            {
                var cluApplication = new CluApplication(
                    configuration["Clu:ProjectName"],
                    configuration["Clu:DeploymentName"],
                    configuration["Clu:APIKey"],
                    "https://" + configuration["Clu:APIHostName"]);
                var recognizerOptions = new CluOptions(cluApplication) { Language = "en" };

                _recognizer = new CluRecognizer(recognizerOptions);
            }
        }

        // Returns true if clu is configured in the appsettings.json and initialized.
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}

