using NetSuiteAccess.Shared;
using System;
using System.Collections.Generic;

namespace NetSuiteAccess.Models
{
	public class PurchaseOrder : Order { }

	public class NetSuitePurchaseOrder : NetSuiteOrder
	{
		public string SupplierName { get; set; }
		public DateTime ShipDate { get; set; }
		public NetSuitePurchaseOrderItem[] Items { get; set; }
		public string PrivateNote { get; set; }
	}

	public class NetSuitePurchaseOrderItem
	{
		public string Sku { get; set; }
		public string Title { get; set; }
		public decimal UnitPrice { get; set; }
		public int Quantity { get; set; }
	}

	public static class PurchaseOrderExtensions
	{
		public static NetSuitePurchaseOrder ToSVPurchaseOrder( this PurchaseOrder order )
		{
			var svPurchaseOrder = new NetSuitePurchaseOrder()
			{
				Id = order.Id,
				DocNumber = order.TranId,
				CreatedDateUtc = order.CreatedDate.FromRFC3339ToUtc(),
				ModifiedDateUtc = order.LastModifiedDate.FromRFC3339ToUtc(),
				Status = order.Status,
				Total = order.Total,
				ShipDate = order.ShipDate,
				PrivateNote = order.Memo
			};

			svPurchaseOrder.ShippingInfo = new NetSuiteShippingInfo();

			if ( order.ShippingAddress != null )
			{
				svPurchaseOrder.ShippingInfo.Address = new NetSuiteShippingAddress()
				{
					Line1 = order.ShippingAddress.Addr1,
					City = order.ShippingAddress.City,
					PostalCode = order.ShippingAddress.Zip,
					CountryCode = order.ShippingAddress.Country,
					State = order.ShippingAddress.State
				};
			}

			if ( order.Entity != null )
			{
				svPurchaseOrder.SupplierName = order.Entity.RefName;
			}

			var items = new List< NetSuitePurchaseOrderItem >();

			if ( order.ItemsInfo != null )
			{
				foreach( var itemInfo in order.ItemsInfo.Items )
				{
					items.Add( new NetSuitePurchaseOrderItem()
					{
						Quantity = (int)Math.Floor( itemInfo.Quantity ),
						Sku = itemInfo.ItemInfo != null ? itemInfo.ItemInfo.RefName : string.Empty,
						Title = itemInfo.Description,
						UnitPrice = itemInfo.Rate
					} );
				}
			}
			svPurchaseOrder.Items = items.ToArray();

			return svPurchaseOrder;
		}

		public static NetSuitePurchaseOrder ToSVPurchaseOrder( this NetSuiteSoapWS.PurchaseOrder order )
		{
			var svPurchaseOrder = new NetSuitePurchaseOrder()
			{
				Id = order.internalId,
				DocNumber = order.tranId,
				CreatedDateUtc = order.createdDate,
				ModifiedDateUtc = order.lastModifiedDate,
				Status = order.status,
				Total = (decimal)order.total,
				ShipDate = order.shipDate,
				PrivateNote = order.memo
			};

			if ( order.entity != null )
			{
				svPurchaseOrder.SupplierName = order.entity.name;
			}

			var items = new List< NetSuitePurchaseOrderItem >();

			if ( order.itemList != null )
			{
				foreach( var item in order.itemList.item )
				{
					items.Add( new NetSuitePurchaseOrderItem()
					{
						 Quantity = (int)item.quantity,
						 Sku = item.item.name,
						 Title = item.description,
						 UnitPrice = decimal.Parse( item.rate )
					} );
				}
			}
			svPurchaseOrder.Items = items.ToArray();

			return svPurchaseOrder;
		}
	}
}