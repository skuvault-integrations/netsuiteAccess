using NetSuiteAccess.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NetSuiteAccess.Models
{
	public class Order
	{
		[ JsonProperty( "id" ) ]
		public long Id { get; set; }
		public string CreatedDate { get; set; }
		[ JsonProperty( "lastModifiedDate" ) ]
		public string LastModifiedDate { get; set; }
		public string Status { get; set; }
		public decimal Total { get; set; }
		[ JsonProperty( "shippingaddress" ) ]
		public ShippingAddress ShippingAddress { get; set; }
		[ JsonProperty( "shipMethod" ) ]
		public RecordMetaInfo ShipMethod { get; set; }
		[ JsonProperty( "shippingCost" ) ]
		public decimal ShippingCost { get; set; }
		[ JsonProperty( "item" ) ]
		public ItemsMetaInfo ItemsInfo { get; set; }
	}

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
		[ JsonProperty( "quantity" ) ]
		public decimal Quantity { get; set; }
	}

	public class NetSuiteOrder
	{
		public long Id { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime ModifiedDate { get; set; }
		public string Status { get; set; }
		public NetSuiteShippingInfo ShippingInfo { get; set; }
		public decimal Total { get; set; }
		public NetSuiteOrderItem[] Items { get; set; }
	}

	public class NetSuiteOrderItem
	{
		public string Sku { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Tax { get; set; }
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
	}

	public static class OrderExtensions
	{
		public static NetSuiteOrder ToSvOrder( this Order order )
		{
			var svOrder = new NetSuiteOrder
			{
				Id = order.Id,
				CreatedDate = order.CreatedDate.FromRFC3339ToUtc(),
				ModifiedDate = order.LastModifiedDate.FromRFC3339ToUtc(),
				Status = order.Status,
				Total = order.Total
			};

			svOrder.ShippingInfo = new NetSuiteShippingInfo()
			{
				Cost = order.ShippingCost
			};

			if ( order.ShippingAddress != null )
			{
				svOrder.ShippingInfo.Address = new NetSuiteShippingAddress()
				{
					Line1 = order.ShippingAddress.Addr1,
					City = order.ShippingAddress.City,
					PostalCode = order.ShippingAddress.Zip,
					CountryCode = order.ShippingAddress.Country,
					State = order.ShippingAddress.State
				};
				svOrder.ShippingInfo.ContactInfo = new NetSuiteShippingContactInfo()
				{
					Name = order.ShippingAddress.Addressee
				};
			}

			if ( order.ShipMethod != null )
			{
				svOrder.ShippingInfo.Carrier = order.ShipMethod.RefName;
			}

			var items = new List< NetSuiteOrderItem >();

			if ( order.ItemsInfo != null )
			{
				foreach( var itemInfo in order.ItemsInfo.Items )
				{
					items.Add( new NetSuiteOrderItem()
					{
						Quantity = (int)Math.Floor( itemInfo.Quantity ),
						Sku = itemInfo.ItemInfo != null ? itemInfo.ItemInfo.RefName : string.Empty
					} );
				}
			}
			svOrder.Items = items.ToArray();

			return svOrder;
		}
	}
}
