// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder;
using Multicloud.Bot.Dialogs;
using Multicloud.Bot.Bots;

namespace Multicloud.Bot
{
    public class Program
    {
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

			var version = builder.Configuration.GetRequiredSection("Version")?.Value
			?? throw new NullReferenceException("The Version value cannot be found. Has the 'Version' environment variable been set correctly for the Web App?");

			if (builder.Environment.IsDevelopment())
			{
				InitializeDevEnvironment(builder, version);
			}
			else
			{
				InitializeProdEnvironment(builder, version);
			}

			builder.Services.AddEndpointsApiExplorer();

			builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
			});

			// Create the Bot Framework Authentication to be used with the Bot Adapter.
			builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

			// Create the Bot Adapter with error handling enabled.
			builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

			// Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
			builder.Services.AddSingleton<IStorage, MemoryStorage>();

			// Create the User state. (Used in this bot's Dialog implementation.)
			builder.Services.AddSingleton<UserState>();

			// Create the Conversation state. (Used by the Dialog system itself.)
			builder.Services.AddSingleton<ConversationState>();

			// Register LUIS recognizer
			builder.Services.AddSingleton<MulticloudRecognizer>();

			// The MainDialog that will be run by the bot.
			builder.Services.AddSingleton<MainDialog>();

			// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
			builder.Services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
			//builder.Services.AddSingleton(async x => await RedisConnection.InitializeAsync(connectionString: builder.Configuration["Cache:ConnectionString"]));

			var app = builder.Build();

			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseWebSockets();
			app.UseRouting();
			app.UseAuthorization();
			//app.UseHttpsRedirection();

			app.MapControllers();

			app.Run();
		}

		private static void InitializeProdEnvironment(WebApplicationBuilder builder, string version)
		{
			// For procution environment, we'll configured Managed Identities for managing access Azure App Services
			// and Key Vault. The Azure App Services endpoint is stored in an environment variable for the web app.

			//logger.LogInformation($"Is Production.");

			var appConfigurationEndpoint = builder.Configuration.GetRequiredSection("AppConfiguration:Endpoint")?.Value
				?? throw new NullReferenceException("The Azure App Configuration Endpoint cannot be found. Has the endpoint environment variable been set correctly for the Web App?");

			// Get the ClientId of the UserAssignedIdentity
			// If we don't set this ClientID in the ManagedIdentityCredential constructor, it doesn't know it should use the user assigned managed id.
			var managedIdentityClientId = builder.Configuration.GetRequiredSection("UserAssignedManagedIdentityClientId")?.Value
				?? throw new NullReferenceException("The Environment Variable 'UserAssignedManagedIdentityClientId' cannot be null. Check the App Service Configuration.");

			ManagedIdentityCredential userAssignedManagedCredentials = new(managedIdentityClientId);

			builder.Configuration.AddAzureAppConfiguration(options =>
				options.Connect(new Uri(appConfigurationEndpoint), userAssignedManagedCredentials)
					.ConfigureKeyVault(kv => kv.SetCredential(userAssignedManagedCredentials))
				.Select(KeyFilter.Any, version)); // <-- Important since we're using labels in our Azure App Configuration store
		}

		private static void InitializeDevEnvironment(WebApplicationBuilder builder, string version)
		{
			// IMPORTANT
			// The current version.
			// Must corresspond exactly to the version string of our deployment as specificed in the deployment config.json.

			//logger.LogInformation($"Is Development.");

			// For local development, use the Secret Manager feature of .NET to store a connection string
			// and likewise for storing a secret for the permission-api app. 
			// https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows

			var appConfigurationconnectionString = builder.Configuration.GetConnectionString("AppConfig")
				?? throw new NullReferenceException("App config missing.");

			// Use the connection string to access Azure App Configuration to get access to app settings stored there.
			// To gain access to Azure Key Vault use 'Azure Cli: az login' to log into Azure.
			// This login on will also now provide valid access tokens to the local development environment.
			// For more details and the option to chain and combine multiple credential options with `ChainedTokenCredential`
			// please see: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet#define-a-custom-authentication-flow-with-chainedtokencredential

			AzureCliCredential credential = new();

			builder.Configuration.AddAzureAppConfiguration(options =>
					options.Connect(appConfigurationconnectionString)
						.ConfigureKeyVault(kv => kv.SetCredential(credential))
					.Select(KeyFilter.Any, version)); // <-- Important: since we're using labels in our Azure App Configuration store

			//logger.LogInformation($"Initialization complete.");
		}
	}
}
