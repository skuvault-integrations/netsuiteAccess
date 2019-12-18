using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class NetSuiteLocation
	{
		[ JsonProperty( "id" ) ]
		public long Id { get; set; }
		[ JsonProperty( "name" ) ]
		public string Name { get; set; }
	}
}
