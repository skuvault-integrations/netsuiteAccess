using NetSuiteSoapWS;

namespace NetSuiteAccess.Models
{
	public class NetSuiteItem
	{
		public string Name { get; set; }
		public string Sku { get; set; }
		public string CategoryName { get; set; }
		public double? Weight { get; set; }
		public string WeightUnit { get; set; }
		public double? Price { get; set; }
		public string PartNumber { get; set; }
		public string Manufacturer { get; set; }
	}

	public static class ItemExtensions
	{
		public static NetSuiteItem ToSVItem( this InventoryItem item )
		{
			var svItem = new NetSuiteItem()
			{
				Name = item.displayName,
				Sku = item.itemId,
				Weight = item.weight,
				WeightUnit = item.weightUnit.ToString(),	
				Manufacturer = item.manufacturer,
				Price = item.cost,
				PartNumber = item.mpn
			};

			if ( item.@class != null )
			{
				svItem.CategoryName = item.@class.name;
			}

			return svItem;
		}

		public static NetSuiteItem ToSVItem( this SerializedInventoryItem serializedInventoryItem )
		{
			var svItem = new NetSuiteItem()
			{
				Name = serializedInventoryItem.displayName,
				Sku = serializedInventoryItem.itemId,
				Weight = serializedInventoryItem.weight,
				WeightUnit = serializedInventoryItem.weightUnit.ToString(),	
				Manufacturer = serializedInventoryItem.manufacturer,
				Price = serializedInventoryItem.cost,
				PartNumber = serializedInventoryItem.mpn
			};

			if ( serializedInventoryItem.@class != null )
			{
				svItem.CategoryName = serializedInventoryItem.@class.name;
			}

			return svItem;
		}

		public static NetSuiteItem ToSVItem( this LotNumberedInventoryItem lotInventoryItem )
		{
			var svItem = new NetSuiteItem()
			{
				Name = lotInventoryItem.displayName,
				Sku = lotInventoryItem.itemId,
				Weight = lotInventoryItem.weight,
				WeightUnit = lotInventoryItem.weightUnit.ToString(),	
				Manufacturer = lotInventoryItem.manufacturer,
				Price = lotInventoryItem.cost,
				PartNumber = lotInventoryItem.mpn
			};

			if ( lotInventoryItem.@class != null )
			{
				svItem.CategoryName = lotInventoryItem.@class.name;
			}

			return svItem;
		}
	}
}