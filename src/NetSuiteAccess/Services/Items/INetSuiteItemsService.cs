﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Items
{
	public interface INetSuiteItemsService
	{
		Task UpdateItemQuantityBySkuAsync( string accountInternalId, string warehouseName, string sku, int quantity, CancellationToken token );
		Task UpdateSkusQuantitiesAsync( string accountInternalId, string warehouseName, Dictionary< string, int > skuQuantities, CancellationToken token );
		Task< int > GetSkuQuantity( string sku, string warehouse, CancellationToken token );
	}
}