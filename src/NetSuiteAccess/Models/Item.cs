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
		public string MPN { get; set; }
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
				MPN = item.mpn
			};

			if ( item.@class != null )
			{
				svItem.CategoryName = item.@class.name;
			}

			return svItem;
		}
	}
}