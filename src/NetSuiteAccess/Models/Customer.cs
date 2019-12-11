using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class NetSuiteCustomer
	{
		[ JsonProperty( "id" ) ]
		public long Id { get; set; }
		[ JsonProperty( "companyName" ) ]
		public string CompanyName { get; set; }
		[ JsonProperty( "firstName" ) ]
		public string FirstName { get; set; }
		[ JsonProperty( "lastName" ) ]
		public string LastName { get; set; }
		[ JsonProperty( "email" ) ]
		public string Email { get; set; }
		[ JsonProperty( "phone" ) ]
		public string Phone { get; set; }
	}
}