using NetSuiteAccess.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NetSuiteAccess.Models
{
	public class SalesOrder : Order
	{
		[ JsonProperty( "shipMethod" ) ]
		public RecordMetaInfo ShipMethod { get; set; }
		[ JsonProperty( "shippingCost" ) ]
		public decimal ShippingCost { get; set; }
	}

	public class NetSuiteSalesOrder : NetSuiteOrder
	{
		public NetSuiteCustomer Customer { get; set; }
		public NetSuiteSalesOrderItem[] Items { get; set; }
	}

	public class NetSuiteSalesOrderItem
	{
		public string Sku { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Tax { get; set; }
		public decimal TaxRate { get; set; }
	}

	public static class OrderExtensions
	{
		public static NetSuiteSalesOrder ToSVSalesOrder( this SalesOrder order )
		{
			var svOrder = new NetSuiteSalesOrder
			{
				Id = order.Id,
				DocNumber = order.TranId,
				CreatedDateUtc = order.CreatedDate.FromRFC3339ToUtc(),
				ModifiedDateUtc = order.LastModifiedDate.FromRFC3339ToUtc(),
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
			}

			if ( order.ShipMethod != null )
			{
				svOrder.ShippingInfo.Carrier = order.ShipMethod.RefName;
			}

			var items = new List< NetSuiteSalesOrderItem >();

			if ( order.ItemsInfo != null )
			{
				foreach( var itemInfo in order.ItemsInfo.Items )
				{
					items.Add( new NetSuiteSalesOrderItem()
					{
						Quantity = (int)Math.Floor( itemInfo.Quantity ),
						Sku = itemInfo.ItemInfo != null ? itemInfo.ItemInfo.RefName : string.Empty,
						UnitPrice = itemInfo.Rate,
						TaxRate = itemInfo.TaxRate,
						Tax = itemInfo.Rate * (itemInfo.TaxRate / 100)
					} );
				}
			}
			svOrder.Items = items.ToArray();

			if ( order.Entity != null )
			{
				svOrder.Customer = new NetSuiteCustomer()
				{
					Id = order.Entity.Id
				};
			}

			return svOrder;
		}
	}
}
