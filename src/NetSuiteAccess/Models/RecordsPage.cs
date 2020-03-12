using System;

namespace NetSuiteAccess.Models
{
	public class RecordsPage
	{
		public Link[] Links { get; set; }
		public int Count { get; set; }
		public bool HasMore { get; set; }
		public RecordMetaInfo[] Items { get; set; }
		public int Offset { get; set; }
		public int TotalResults { get; set; }
	}
}