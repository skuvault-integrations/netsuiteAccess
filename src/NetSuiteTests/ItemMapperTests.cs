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
		public void InventoryItemToSVItem()
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
			svItem.PartNumber.Should().Be( item.mpn );
		}

		[ Test ]
		public void LotNumberedItemToSVItem()
		{
			var item = new LotNumberedInventoryItem()
			{
				itemId = "NS-testskuLot-1",
				displayName = "NS-testskuLot-1",
				weight = 14.0,
				weightUnit = ItemWeightUnit._g,
				manufacturer = "Apple",
				cost = 10.0,
				mpn = "1234"
			};

			var svItem = item.ToSVItem();

			svItem.Name.Should().Be( item.displayName );
			svItem.Sku.Should().Be( item.itemId );
			svItem.Weight.Should().Be( item.weight );
			svItem.WeightUnit.Should().Be( item.weightUnit.ToString() );
			svItem.Manufacturer.Should().Be( item.manufacturer );
			svItem.Price.Should().Be( item.cost );
			svItem.PartNumber.Should().Be( item.mpn );
		}

		[ Test ]
		public void SerializedItemToSVItem()
		{
			var item = new SerializedInventoryItem()
			{
				itemId = "NS-testskuSerialized-1",
				displayName = "NS-testskuSerialized-1",
				weight = 9.0,
				weightUnit = ItemWeightUnit._kg,
				manufacturer = "HP",
				cost = 12.0,
				mpn = "9876"
			};

			var svItem = item.ToSVItem();

			svItem.Name.Should().Be( item.displayName );
			svItem.Sku.Should().Be( item.itemId );
			svItem.Weight.Should().Be( item.weight );
			svItem.WeightUnit.Should().Be( item.weightUnit.ToString() );
			svItem.Manufacturer.Should().Be( item.manufacturer );
			svItem.Price.Should().Be( item.cost );
			svItem.PartNumber.Should().Be( item.mpn );
		}
	}
}