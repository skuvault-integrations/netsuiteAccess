using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class ItemsMetaInfo
	{
		[ JsonProperty( "links" ) ]
		public Link[] Links { get; set; }
		[ JsonProperty( "items" ) ]
		public ItemMetaInfo[] Items { get; set; }
	}

	public class ItemMetaInfo
	{
		[ JsonProperty( "item" ) ]
		public RecordMetaInfo ItemInfo { get; set; }
		[ JsonProperty( "description" ) ]
		public string Description { get; set; }
		[ JsonProperty( "quantity" ) ]
		public decimal Quantity { get; set; }
		[ JsonProperty( "rate" ) ]
		public decimal Rate { get; set; }
		[ JsonProperty( "taxRate1" ) ]
		public decimal TaxRate { get; set; }
		[ JsonProperty( "amount" ) ]
		public decimal Cost { get; set; }
	}
}