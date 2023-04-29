using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Interfaces.AzureStorage
{
	public interface IAzureTableService
	{
		Task StoreUserDetailsAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken);

	}
}
