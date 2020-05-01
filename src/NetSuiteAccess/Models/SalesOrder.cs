using NetSuiteAccess.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

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
		public NetSuiteSalesOrderSource Source { get; set; }
		public NetSuiteSalesOrderStatus Status { get; set; }
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

	public enum NetSuiteSalesOrderStatus
	{
		Unknown,
		PendingApproval,
		PendingBilling,
		PendingBillingPartFulfilled,
		PartiallyFulfilled,
		Billed,
		PendingFulfillment,
		Cancelled,
		Closed
	}

	public enum NetSuiteSalesOrderSource
	{
		NetSuite,
		External
	}

	public static class OrderExtensions
	{
		public static Dictionary< string, NetSuiteSalesOrderStatus > SalesOrderStatuses { get; private set; }

		static OrderExtensions()
		{
			SalesOrderStatuses = new Dictionary< string, NetSuiteSalesOrderStatus >
			{
				{ "Pending Approval", NetSuiteSalesOrderStatus.PendingApproval },
				{ "Pending Billing", NetSuiteSalesOrderStatus.PendingBilling },
				{ "Pending BillingPart Fulfilled", NetSuiteSalesOrderStatus.PendingBillingPartFulfilled },
				{ "Partially Fulfilled", NetSuiteSalesOrderStatus.PartiallyFulfilled },
				{ "Billed", NetSuiteSalesOrderStatus.Billed },
				{ "Pending Fulfillment", NetSuiteSalesOrderStatus.PendingFulfillment },
				{ "Cancelled", NetSuiteSalesOrderStatus.Cancelled },
				{ "Closed", NetSuiteSalesOrderStatus.Closed }
			};
		}

		public static NetSuiteSalesOrder ToSVSalesOrder( this SalesOrder order )
		{
			var svOrder = new NetSuiteSalesOrder
			{
				Id = order.Id,
				DocNumber = order.TranId,
				CreatedDateUtc = order.CreatedDate.FromRFC3339ToUtc(),
				ModifiedDateUtc = order.LastModifiedDate.FromRFC3339ToUtc(),
				Status = GetSalesOrderStatus( order.Status ),
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

		public static NetSuiteSalesOrder ToSVSalesOrder( this NetSuiteSoapWS.SalesOrder order )
		{
			var svOrder = new NetSuiteSalesOrder
			{
				Id = order.internalId,
				DocNumber = order.tranId,
				CreatedDateUtc = order.createdDate.ToUniversalTime(),
				ModifiedDateUtc = order.lastModifiedDate.ToUniversalTime(),
				Status = GetSalesOrderStatus( order.status ),
				Total = (decimal)order.total
			};

			if ( !string.IsNullOrWhiteSpace( order.source ) 
					&& order.source.Equals( "Web Services" ) )
			{
				svOrder.Source = NetSuiteSalesOrderSource.External;
			}

			svOrder.ShippingInfo = new NetSuiteShippingInfo()
			{
				Cost = (decimal)order.shippingCost
			};

			if ( order.shippingAddress != null )
			{
				svOrder.ShippingInfo.Address = new NetSuiteShippingAddress()
				{
					Line1 = order.shippingAddress.addr1,
					Line2 = order.shippingAddress.addr2,
					City = order.shippingAddress.city,
					PostalCode = order.shippingAddress.zip,
					CountryCode = order.shippingAddress.country.ToString(),
					State = order.shippingAddress.state
				};
			}

			if ( order.shipMethod != null )
			{
				svOrder.ShippingInfo.Carrier = order.shipMethod.name;
			}

			var items = new List< NetSuiteSalesOrderItem >();
			if ( order.itemList != null )
			{
				foreach( var itemInfo in order.itemList.item )
				{
					items.Add( new NetSuiteSalesOrderItem()
					{
						Quantity = (int)Math.Floor( itemInfo.quantity ),
						Sku = itemInfo.item != null ? itemInfo.item.name : string.Empty,
						UnitPrice = GetOrderLineItemUnitPrice( itemInfo ),
						TaxRate = (decimal)itemInfo.taxRate1,
						Tax = (decimal)itemInfo.taxAmount
					} );
				}
			}
			svOrder.Items = items.ToArray();

			svOrder.Customer = new NetSuiteCustomer()
			{
				Id = int.Parse( order.entity.internalId )
			};

			return svOrder;
		}

		private static decimal GetOrderLineItemUnitPrice( NetSuiteSoapWS.SalesOrderItem saleOrderItem )
		{
			decimal unitPrice = 0;

			if ( saleOrderItem.amountSpecified && saleOrderItem.quantity > 0 )
			{
				unitPrice = (decimal)Math.Round( saleOrderItem.amount / saleOrderItem.quantity, 2 );
			}

			return unitPrice;
		}

		private static NetSuiteSalesOrderStatus GetSalesOrderStatus( string status )
		{
			if ( string.IsNullOrWhiteSpace( status ) )
				return NetSuiteSalesOrderStatus.Unknown;

			if ( !SalesOrderStatuses.TryGetValue( status, out NetSuiteSalesOrderStatus salesOrderStatus ) )
			{
				return NetSuiteSalesOrderStatus.Unknown;
			}

			return salesOrderStatus;
		}
	}
}
