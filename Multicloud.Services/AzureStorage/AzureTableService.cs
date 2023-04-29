using Azure;
using Azure.Data.Tables;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Multicloud.Interfaces.AzureStorage;
using Multicloud.Models.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Services.AzureStorage
{
	public class AzureTableService : IAzureTableService
	{
		private readonly IConfiguration _configuration;

		public AzureTableService(IConfiguration configuration) { _configuration = configuration;}

		// Store teams user details in Azure Table Storage
		public async Task StoreUserDetailsAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
		{
			if (turnContext.Activity.ChannelId == "msteams")
			{
				try
				{
					switch (turnContext.Activity.Conversation.ConversationType)
					{
						case "personal":
							// get the teams user details
							TeamsChannelAccount teamsUser = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);

							if (teamsUser != null && !teamsUser.UserPrincipalName.ToLower().Contains("#ext#"))
							{
								if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
								{
									throw new Exception("NOTE: Storage Account is not configured.");
								}

								var tableClient = await GetTableClient(_configuration["TableData:StorageAccountUserTable"]);

								// <create_object_add> 
								// Create new item using composite key constructor
								var teamsUserDetails = new UserDetailsEntity()
								{
									RowKey = turnContext.Activity.From.AadObjectId,
									PartitionKey = turnContext.Activity.Conversation.TenantId,
									DisplayName = teamsUser.Name,
									Email = teamsUser.Email.ToLower(),
									UserID = turnContext.Activity.From.Id,
									Role = turnContext.Activity.From.Role,
									ConversationID = turnContext.Activity.Conversation.Id,
									ServiceURL = turnContext.Activity.ServiceUrl
								};

								// Add new item to server-side table
								await tableClient.UpsertEntityAsync<UserDetailsEntity>(teamsUserDetails);
								// </create_object_add>
							}
							else
							{
								await turnContext.SendActivityAsync(MessageFactory.Text("I am having some trouble getting your teams profile. Please contact your Administrator."));

								throw new Exception("NOTE: Teams User Details not found.");
							}

							break;

						case "channel":
							// Gets the details for the given team id. This only works in team scoped conversations.
							// TeamsGetTeamInfo: Gets the TeamsInfo object from the current activity.
							//TeamDetails teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, turnContext.Activity.TeamsGetTeamInfo().Id);
							//if (teamDetails != null)
							//{
							//	if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
							//	{
							//		throw new Exception("NOTE: Storage Account is not configured for Tweet.");
							//	}

							//	var tableClient = await GetTableClient(_configuration["TableData:StorageAccountGroupTable"]);

							//	// <create_object_add> 
							//	// Create new item using composite key constructor
							//	var teamsGroupDetails = new GroupDetails()
							//	{
							//		RowKey = teamDetails.Id,
							//		PartitionKey = dc.Context.Activity.Conversation.TenantId,
							//		TeamName = teamDetails.Name,
							//		AadGroupID = teamDetails.AadGroupId,
							//		ServiceURL = dc.Context.Activity.ServiceUrl,
							//	};

							//	// Add new item to server-side table
							//	await tableClient.UpsertEntityAsync<GroupDetails>(teamsGroupDetails);
							//	// </create_object_add>

							//	return await dc.ContinueDialogAsync();
							//}
							//else
							//{
							//	await dc.Context.SendActivityAsync(MessageFactory.Text("I am having some trouble getting teams profile. Please contact your Administrator."));

							//	throw new Exception("NOTE: Teams Group Details not found.");
							//}

							break;

						default: break;
					}
				}
				catch (Exception)
				{
					await turnContext.SendActivityAsync(MessageFactory.Text("I am having some trouble getting your teams profile. Please contact your Administrator."));
					throw;
				}
			}
		}

		// Get Table Client and create table if not exists
		private async Task<TableClient> GetTableClient(string tableName)
		{
			// <client_credentials> 
			// New instance of the TableClient class
			TableServiceClient tableServiceClient = new TableServiceClient(_configuration["ConnectionStrings:StorageAccount"]);
			// </client_credentials>

			// <create_table>
			// New instance of TableClient class referencing the server-side table
			TableClient tableClient = tableServiceClient.GetTableClient(
				tableName: tableName
			);

			await tableClient.CreateIfNotExistsAsync();
			// </create_table>

			return tableClient;
		}

		// store the data in the table storage
		public async Task<bool> InsertEntityAsync<T>(string tableName, T data) where T : class, ITableEntity, new()
		{
			try
			{
				if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
				{
					throw new Exception("NOTE: Storage Account is not configured.");
				}

				var tableClient = await GetTableClient(tableName);

				// Add new item to server-side table
				await tableClient.UpsertEntityAsync<T>(data);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		// get the data from the table storage
		public async Task<Response<T>> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
		{
			try
			{
				if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
				{
					throw new Exception("NOTE: Storage Account is not configured.");
				}

				var tableClient = await GetTableClient(tableName);
				return await tableClient.GetEntityAsync<T>(partitionKey: partitionKey, rowKey: rowKey);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		// get all data from the table storage
		public async Task<List<T>> GetEntitiesAsync<T>(string tableName, FormattableString filter) where T : class, ITableEntity, new()
		{
			try
			{
				if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
				{
					throw new Exception("NOTE: Storage Account is not configured.");
				}

				var tableClient = await GetTableClient(tableName);
				var allData = tableClient.Query<T>(filter: TableClient.CreateQueryFilter(filter)).ToList();

				return allData;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
	}
}
