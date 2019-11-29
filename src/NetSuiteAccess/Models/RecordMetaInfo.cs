using System;
using System.Collections.Generic;
using System.Text;

namespace NetSuiteAccess.Models
{
	public class RecordMetaInfo
	{
		public Link[] Links { get; set; }
		public long Id { get; set; }
		public string RefName { get; set; }
	}

	public class Link
	{
		public string Rel { get; set; }
		public string Href { get; set; }
	}
}
