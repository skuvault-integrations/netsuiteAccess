using System.Collections.Generic;

namespace NetSuiteAccess.Services.Items
{
	public class NetSuiteItemQuantity
	{
		public int? AvailableQuantity;
		public IEnumerable< NetSuiteBinQuantity > BinQuantities;

		public NetSuiteItemQuantity()
		{
			this.BinQuantities = new List< NetSuiteBinQuantity >();
		}
	}

	public class NetSuiteBinQuantity
	{
		public string LocationName;	
		public string BinNumber;
		public int Quantity;

		public NetSuiteBinQuantity( string locationName, string binNumber, int quantity )
		{
			this.LocationName = locationName;
			this.BinNumber = binNumber;
			this.Quantity = quantity;
		}
	}
}
