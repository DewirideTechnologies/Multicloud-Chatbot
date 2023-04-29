using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Services.Cards
{
	public class CardsService
	{
		// create adaptive card with json file path
		public static Attachment CreateAdaptiveCardAttachment(string[] path)
		{
			string templateJson = System.IO.File.ReadAllText(Path.Combine(path), Encoding.UTF8);

			var adaptiveCardAttachment = new Attachment()
			{
				ContentType = "application/vnd.microsoft.card.adaptive",
				Content = JsonConvert.DeserializeObject(templateJson),
			};

			return adaptiveCardAttachment;
		}

		// create adaptive card with json file path and data
		public static Attachment CreateAdaptiveCardAttachment(string[] path, object dataJson)
		{
			string templateJson = System.IO.File.ReadAllText(Path.Combine(path), Encoding.UTF8);
			var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);
			var card = template.Expand(dataJson);

			var adaptiveCardAttachment = new Attachment()
			{
				ContentType = "application/vnd.microsoft.card.adaptive",
				Content = JsonConvert.DeserializeObject(card),
			};

			return adaptiveCardAttachment;
		}

	}
}
