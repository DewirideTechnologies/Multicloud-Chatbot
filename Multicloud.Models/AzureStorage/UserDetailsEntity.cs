using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Models.AzureStorage
{
	public class UserDetailsEntity : CommonEntity
	{
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string UserID { get; set; }
		public string Role { get; set; }
		public string ConversationID { get; set; }
		public string ServiceURL { get; set; }
	}
}
