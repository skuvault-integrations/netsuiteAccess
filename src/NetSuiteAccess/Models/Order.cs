using Newtonsoft.Json;
using System;

namespace NetSuiteAccess.Models
{
	public abstract class Order
	{
		[ JsonProperty( "id" ) ]
		public long Id { get; set; }
		[ JsonProperty( "tranId" ) ]
		public long TranId { get; set; }
		public string CreatedDate { get; set; }
		[ JsonProperty( "lastModifiedDate" ) ]
		public string LastModifiedDate { get; set; }
		public string Status { get; set; }
		public decimal Total { get; set; }
		[ JsonProperty( "shippingaddress" ) ]
		public ShippingAddress ShippingAddress { get; set; }
		[ JsonProperty( "shipDate" ) ]
		public DateTime ShipDate { get; set; }
		[ JsonProperty( "email" ) ]
		public string Email { get; set; }
		[ JsonProperty( "item" ) ]
		public ItemsMetaInfo ItemsInfo { get; set; }
	}

	public abstract class NetSuiteOrder
	{
		public long Id { get; set; }
		public long DocNumber { get; set; }
		public DateTime CreatedDateUtc { get; set; }
		public DateTime ModifiedDateUtc { get; set; }
		public string Status { get; set; }
		public NetSuiteShippingInfo ShippingInfo { get; set; }
		public decimal Total { get; set; }
	}

	public class NetSuiteShippingInfo
	{
		public NetSuiteShippingContactInfo ContactInfo { get; set; }
		public NetSuiteShippingAddress Address { get; set; }
		public string Carrier { get; set; }
		public decimal Cost { get; set; }
	}

	public class NetSuiteShippingAddress
	{
		public string CountryCode { get; set; }
		public string State { get; set; }
		public string Region { get; set; }
		public string PostalCode { get; set; }
		public string City { get; set; }
		public string Line1 { get; set; }
	}

	public class NetSuiteShippingContactInfo
	{
		public string Name { get; set; }
		public string Email { get; set; }
	}
}
