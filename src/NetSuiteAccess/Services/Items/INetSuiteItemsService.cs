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
		Task UpdateItemQuantityBySkuAsync( int accountId, string warehouseName, string sku, int quantity, CancellationToken token );
		Task UpdateSkusQuantitiesAsync( int accountId, string warehouseName, Dictionary< string, int > skuQuantities, CancellationToken token, Mark mark );
		Task< int > GetSkuQuantity( string sku, string warehouse, CancellationToken token );
		Task< IEnumerable< NetSuiteItem > > GetItemsCreatedUpdatedAfterAsync( DateTime startDateUtc, bool includeUpdated, CancellationToken token, Mark mark );
	}
}