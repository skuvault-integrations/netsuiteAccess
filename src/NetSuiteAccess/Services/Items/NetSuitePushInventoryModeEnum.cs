namespace NetSuiteAccess.Services.Items
{
	public enum NetSuitePushInventoryModeEnum
	{
		/// <summary>
		/// Push quantity to items if the selected NetSuite location doesn't Use Bins or the item doesn't Use Bins
		/// </summary>
		ItemsNotInBins,	

		/// <summary>
		/// Push quantity to bins in items if the selected NetSuite location Uses Bins and the item Uses Bins
		/// </summary>
		ItemsInBins,

		Both
	}
}
