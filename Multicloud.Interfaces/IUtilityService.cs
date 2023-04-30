using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Interfaces
{
    public interface IUtilityService
    {
		Task GetCustomQAResponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken);
		Task SendWelcomeCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken);
		Task SendWelcomeCardAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken);
	}
}
