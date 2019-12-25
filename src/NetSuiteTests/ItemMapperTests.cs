using FluentAssertions;
using NetSuiteAccess.Models;
using NetSuiteSoapWS;
using NUnit.Framework;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class ItemMapperTests
	{
		[ Test ]
		public void ToSVItem()
		{
			var item = new InventoryItem()
			{
				itemId = "NS-testsku1",
				displayName = "NS-testsku1",
				weight = 10.0,
				weightUnit = ItemWeightUnit._lb,
				manufacturer = "Samsung",
				cost = 15.0,
				mpn = "7209101"
			};

			var svItem = item.ToSVItem();

			svItem.Name.Should().Be( item.displayName );
			svItem.Sku.Should().Be( item.itemId );
			svItem.Weight.Should().Be( item.weight );
			svItem.WeightUnit.Should().Be( item.weightUnit.ToString() );
			svItem.Manufacturer.Should().Be( item.manufacturer );
			svItem.Price.Should().Be( item.cost );
			svItem.MPN.Should().Be( item.mpn );
		}
	}
}