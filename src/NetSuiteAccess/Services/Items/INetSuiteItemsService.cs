using Netco.Logging;
using NetSuiteAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Items
{
	public interface INetSuiteItemsService
	{
		Task UpdateItemQuantityBySkuAsync( int accountId, string locationName, string sku, int quantity, NetSuitePushInventoryModeEnum pushInventoryModeEnum, CancellationToken token, Mark mark, string binName = "" );
		Task UpdateSkusQuantitiesAsync( int accountId, string locationName, Dictionary< string, Dictionary< string, int > > skuBinQuantities, NetSuitePushInventoryModeEnum pushInventoryModeEnum, CancellationToken token, Mark mark );
		Task< int > GetItemQuantityAsync( string sku, string locationName, CancellationToken token );
		Task< IEnumerable< NetSuiteItem > > GetItemsCreatedUpdatedAfterAsync( DateTime startDateUtc, bool includeUpdated, CancellationToken token, Mark mark );
	}
}