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
		public void ToSVOrder()
		{
			var order = new Order()
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
							}
						}
					}
				}
			};

			var result = order.ToSvOrder();
			result.Should().NotBeNull();
			result.Id.Should().Be( order.Id );
			result.CreatedDate.Should().Be( order.CreatedDate.FromRFC3339ToUtc() );
			result.ModifiedDate.Should().Be( order.LastModifiedDate.FromRFC3339ToUtc() );
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
		}
	}
}