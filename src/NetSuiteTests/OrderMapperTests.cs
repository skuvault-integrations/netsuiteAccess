using FluentAssertions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Shared;
using NUnit.Framework;
using System;
using System.Linq;
using NetSuiteSoapWS;
using PurchaseOrder = NetSuiteAccess.Models.PurchaseOrder;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class OrderMapperTests
	{
		[ Test ]
		public void WhenToSVSalesOrder_ThenOrderFieldsAreMapped()
		{
			const string discountName = "FOR NICE PEOPLE ONLY";
			const string discountRate = "1000%";
			const double discountTotal = 10.23;
			const double taxTotal = 3.23;
			const string entityInternalId = "12";
			const string internalId = "45";
			const string tranId = "78";
			var createdDate = new DateTime( 1000, 1, 1 );
			var lastModifiedDate = new DateTime( 4000, 1, 1 );
			const string statusPendingBilling = "Pending Billing";
			const double total = 203;
			const string sourceWebServices = "Web Services";
			var order = new NetSuiteSoapWS.SalesOrder
			{
				entity = new RecordRef
				{
					internalId = entityInternalId
				},
				internalId = internalId,
				tranId = tranId,
				createdDate = createdDate,
				lastModifiedDate = lastModifiedDate,
				status = statusPendingBilling,
				total = total,
				source = sourceWebServices,
				discountItem = new RecordRef
				{
					name = discountName
				},
				discountTotal = discountTotal,
				discountRate = discountRate,
				taxTotal = taxTotal
			};

			var result = order.ToSVSalesOrder();

			result.Id.Should().Be( internalId );
			result.DocNumber.Should().Be( tranId );
			result.CreatedDateUtc.Should().Be( createdDate.ToUniversalTime() );
			result.ModifiedDateUtc.Should().Be( lastModifiedDate.ToUniversalTime() );
			result.Status.Should().Be( NetSuiteSalesOrderStatus.PendingBilling );
			result.Total.Should().Be( ( decimal ) total );
			result.Source.Should().Be( NetSuiteSalesOrderSource.External );
			result.Customer.Id.ToString().Should().Be( entityInternalId );
			result.DiscountName.Should().Be( discountName );
			result.DiscountTotal.Should().Be( ( decimal ) discountTotal );
			result.DiscountType.Should().Be( NetSuiteDiscountTypeEnum.Percentage );
			result.TaxTotal.Should().Be( ( decimal ) taxTotal );
		}

		[ Test ]
		public void WhenToSVSalesOrder_ThenOrderShippingIsMapped()
		{
			const double shippingCost = 12.30;
			const string addr1 = "123 Some St";
			const string addr2 = "Apt 2";
			const string city = "Mayberry";
			const string zip = "12334";
			const Country country = Country._afghanistan;
			const string state = "AZ";
			const string shippingMethod = "Unladen sparrow";
			var order = new NetSuiteSoapWS.SalesOrder
			{
				entity = new RecordRef { internalId = "12" },
				shippingCost = shippingCost,
				shipMethod = new RecordRef
				{
					name = shippingMethod
				},
				shippingAddress = new Address
				{
					addr1 = addr1,
					addr2 = addr2,
					city = city,
					zip = zip,
					country = country,
					state = state
				}
			};

			var result = order.ToSVSalesOrder();

			result.ShippingInfo.Cost.Should().Be( ( decimal ) shippingCost );
			result.ShippingInfo.Carrier.Should().Be( shippingMethod );
			var resultShippingAddress = result.ShippingInfo.Address;
			resultShippingAddress.Line1.Should().Be( addr1 );
			resultShippingAddress.Line2.Should().Be( addr2 );
			resultShippingAddress.City.Should().Be( city );
			resultShippingAddress.PostalCode.Should().Be( zip );
			resultShippingAddress.CountryCode.Should().Be( country.ToString() );
			resultShippingAddress.State.Should().Be( state );
		}

		[ Test ]
		public void WhenToSVSalesOrder_ThenOrderItemsAreMapped()
		{
			const double quantity = 3;
			const string itemName = "testSku";
			const double taxRate1 = 2.5;
			const double taxAmount = 4.56;
			const double amount = 11.22;
			var salesOrderItem = new SalesOrderItem
			{
				quantity = quantity,
				item = new RecordRef
				{
					name = itemName
				},
				amountSpecified = true,
				amount = amount,
				taxRate1 = taxRate1,
				taxAmount = taxAmount
			};
			var order = new NetSuiteSoapWS.SalesOrder
			{
				entity = new RecordRef { internalId = "12" },
				itemList = new SalesOrderItemList
				{
					item = new []
					{
						salesOrderItem,
						new SalesOrderItem()
					}
				}
			};

			var result = order.ToSVSalesOrder();
			result.Items.Length.Should().Be( order.itemList.item.Length );
			var resultFirstItem = result.Items [0];
			resultFirstItem.Quantity.Should().Be( ( int ) salesOrderItem.quantity );
			resultFirstItem.Sku.Should().Be( itemName );
			resultFirstItem.UnitPrice.Should().Be( ( decimal )Math.Round( amount / quantity, 2 ) );
			resultFirstItem.TaxRate.Should().Be( ( decimal ) taxRate1 );
			resultFirstItem.TaxAmount.Should().Be( ( decimal ) taxAmount );
		}

		[ Test ]
		public void WhenToDiscountType_GivenDiscountRateIsBlank_ThenUnknown()
		{
			const string taxRateBlank = null;

			var result = taxRateBlank.ToDiscountType();

			result.Should().Be( NetSuiteDiscountTypeEnum.Undefined );
		}

		[ Test ]
		public void WhenToDiscountType_GivenDiscountRateHasPercent_ThenPercent()
		{
			const string taxRateWithPercent = "10%";

			var result = taxRateWithPercent.ToDiscountType();

			result.Should().Be( NetSuiteDiscountTypeEnum.Percentage );
		}

		[ Test ]
		public void WhenToDiscountType_GivenDiscountRateDoesntHavePercent_ThenFixedRate()
		{
			const string taxRateWithoutPercent = "1.20";

			var result = taxRateWithoutPercent.ToDiscountType();

			result.Should().Be( NetSuiteDiscountTypeEnum.FixedAmount );
		}

		[ Test ]
		public void ToSVPurchaseOrder()
		{
			var order = new PurchaseOrder()
			{
				Id = "1",
				CreatedDate = "2019-11-29T10:00:00Z",
				LastModifiedDate = "2019-11-29T10:00:00Z",
				Status = "Pending Receipt",
				Total = 10,
				ShippingAddress = new ShippingAddress()
				{
					Addr1 = "123 Bing Bong Lane",
					Addressee = "SkuVault",
					City = "Lousiville",
					Country = "US",
					State = "KY",
					Zip = "40206"
				},
				ShipDate = new DateTime( 2019, 12, 6 ),
				ItemsInfo = new ItemsMetaInfo()
				{
					Items = new ItemMetaInfo[]
					{
						new ItemMetaInfo()
						{
							Quantity = 10,
							ItemInfo = new RecordMetaInfo()
							{
								Id = 1050,
								RefName = "product555"
							},
							Description = "Test product",
							Rate = 5
						}
					}
				},
				Entity = new RecordMetaInfo()
				{
					Id = 1,
					RefName = "Samsung"
				}
			};

			var result = order.ToSVPurchaseOrder();
			result.Should().NotBeNull();
			result.Id.Should().Be( order.Id );
			result.CreatedDateUtc.Should().Be( order.CreatedDate.FromRFC3339ToUtc() );
			result.ModifiedDateUtc.Should().Be( order.LastModifiedDate.FromRFC3339ToUtc() );
			result.Status.Should().Be( NetSuitePurchaseOrderStatus.PendingReceipt );
			
			result.ShippingInfo.Address.Should().NotBeNull();
			result.ShippingInfo.Address.Line1.Should().Be( order.ShippingAddress.Addr1 );
			result.ShippingInfo.Address.CountryCode.Should().Be( order.ShippingAddress.Country );
			result.ShippingInfo.Address.City.Should().Be( order.ShippingAddress.City );
			result.ShippingInfo.Address.PostalCode.Should().Be( order.ShippingAddress.Zip );
			result.ShipDate.Should().Be( order.ShipDate );

			result.SupplierName.Should().Be( order.Entity.RefName );
			
			result.Items.Count().Should().Be( 1 );
			result.Items[0].Quantity.Should().Be( (int)order.ItemsInfo.Items[0].Quantity );
			result.Items[0].Sku.Should().Be( order.ItemsInfo.Items[0].ItemInfo.RefName );
			result.Items[0].Title.Should().Be( order.ItemsInfo.Items[0].Description );
			result.Items[0].UnitPrice.Should().Be( order.ItemsInfo.Items[0].Rate );
		}
	}
}