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
		Task UpdateItemQuantityBySkuAsync( int accountId, string locationName, string sku, int quantity, NetSuiteInventoryBinsModeEnum inventoryBinsModeEnum, CancellationToken token, Mark mark );
		Task UpdateSkusQuantitiesAsync( int accountId, string locationName, IDictionary< string, NetSuiteItemQuantity > skuLocationQuantities, NetSuiteInventoryBinsModeEnum inventoryBinsModeEnum, CancellationToken token, Mark mark );
		Task< int > GetItemQuantityAsync( string sku, string locationId, CancellationToken token );
		Task< IEnumerable< NetSuiteItem > > GetItemsCreatedUpdatedAfterAsync( DateTime startDateUtc, bool includeUpdated, CancellationToken token, Mark mark );
	}
}