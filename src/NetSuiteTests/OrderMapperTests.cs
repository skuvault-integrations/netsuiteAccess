using FluentAssertions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Shared;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class OrderMapperTests
	{
		[ Test ]
		public void ToSVSalesOrder()
		{
			var order = new SalesOrder()
			{
				Id = 1,
				CreatedDate = "2019-11-29T10:00:00Z",
				LastModifiedDate = "2019-11-29T10:00:00Z",
				ShipMethod = new RecordMetaInfo()
				{
					Id = 732,
					RefName = "USPS Parcel Post"
				},
				Status = "Pending Fulfillment",
				Total = 10,
				ShippingCost = 2,
				ShippingAddress = new ShippingAddress()
				{
					Addr1 = "123 Bing Bong Lane",
					Addressee = "SkuVault",
					City = "Lousiville",
					Country = "US",
					State = "KY",
					Zip = "40206"
				},
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
							TaxRate = 10,
							Rate = 15
						}
					}
				}
			};

			var result = order.ToSVSalesOrder();
			result.Should().NotBeNull();
			result.Id.Should().Be( order.Id );
			result.CreatedDateUtc.Should().Be( order.CreatedDate.FromRFC3339ToUtc() );
			result.ModifiedDateUtc.Should().Be( order.LastModifiedDate.FromRFC3339ToUtc() );
			result.Status.Should().Be( order.Status );
			
			result.ShippingInfo.Should().NotBeNull();
			result.ShippingInfo.Carrier.Should().Be( order.ShipMethod.RefName );
			result.ShippingInfo.Cost.Should().Be( order.ShippingCost );
			
			result.ShippingInfo.ContactInfo.Should().NotBeNull();
			result.ShippingInfo.ContactInfo.Name.Should().Be( order.ShippingAddress.Addressee );

			result.ShippingInfo.Address.Should().NotBeNull();
			result.ShippingInfo.Address.Line1.Should().Be( order.ShippingAddress.Addr1 );
			result.ShippingInfo.Address.CountryCode.Should().Be( order.ShippingAddress.Country );
			result.ShippingInfo.Address.City.Should().Be( order.ShippingAddress.City );
			result.ShippingInfo.Address.PostalCode.Should().Be( order.ShippingAddress.Zip );
			
			result.Items.Count().Should().Be( 1 );
			result.Items[0].Quantity.Should().Be( (int)order.ItemsInfo.Items[0].Quantity );
			result.Items[0].Sku.Should().Be( order.ItemsInfo.Items[0].ItemInfo.RefName );
			result.Items[0].UnitPrice.Should().Be( order.ItemsInfo.Items[0].Rate );
			result.Items[0].TaxRate.Should().Be( order.ItemsInfo.Items[0].TaxRate );
			result.Items[0].Tax.Should().Be( order.ItemsInfo.Items[0].TaxRate / 100 * order.ItemsInfo.Items[0].Rate );
		}

		[ Test ]
		public void ToSVPurchaseOrder()
		{
			var order = new PurchaseOrder()
			{
				Id = 1,
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
				}
			};

			var result = order.ToSVPurchaseOrder();
			result.Should().NotBeNull();
			result.Id.Should().Be( order.Id );
			result.CreatedDateUtc.Should().Be( order.CreatedDate.FromRFC3339ToUtc() );
			result.ModifiedDateUtc.Should().Be( order.LastModifiedDate.FromRFC3339ToUtc() );
			result.Status.Should().Be( order.Status );
			
			result.ShippingInfo.ContactInfo.Should().NotBeNull();
			result.ShippingInfo.ContactInfo.Name.Should().Be( order.ShippingAddress.Addressee );

			result.ShippingInfo.Address.Should().NotBeNull();
			result.ShippingInfo.Address.Line1.Should().Be( order.ShippingAddress.Addr1 );
			result.ShippingInfo.Address.CountryCode.Should().Be( order.ShippingAddress.Country );
			result.ShippingInfo.Address.City.Should().Be( order.ShippingAddress.City );
			result.ShippingInfo.Address.PostalCode.Should().Be( order.ShippingAddress.Zip );
			result.ShipDate.Should().Be( order.ShipDate );
			
			result.Items.Count().Should().Be( 1 );
			result.Items[0].Quantity.Should().Be( (int)order.ItemsInfo.Items[0].Quantity );
			result.Items[0].Sku.Should().Be( order.ItemsInfo.Items[0].ItemInfo.RefName );
			result.Items[0].Title.Should().Be( order.ItemsInfo.Items[0].Description );
			result.Items[0].UnitPrice.Should().Be( order.ItemsInfo.Items[0].Rate );
		}
	}
}