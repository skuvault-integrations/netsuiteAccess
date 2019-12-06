using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class ShippingAddress
	{
		[ JsonProperty( "addressee" ) ]
		public string Addressee { get; set; }
		[ JsonProperty( "addr1" ) ]
		public string Addr1 { get; set; }
		[ JsonProperty( "city" ) ]
		public string City { get; set; }
		[ JsonProperty( "country" ) ]
		public string Country { get; set; }
		[ JsonProperty( "state" ) ]
		public string State { get; set; }
		[ JsonProperty( "zip" ) ]
		public string Zip { get; set; }
	}
}
