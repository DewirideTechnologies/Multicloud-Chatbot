using Azure;
using Azure.Data.Tables;

namespace Multicloud.Models.AzureStorage
{
	public class CommonEntity : ITableEntity
	{
		public string RowKey { get; set; } = default!;

		public string PartitionKey { get; set; } = default!;

		public ETag ETag { get; set; } = default!;

		public DateTimeOffset? Timestamp { get; set; } = default!;
	}
}
