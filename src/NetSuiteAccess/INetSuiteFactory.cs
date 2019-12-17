using NetSuiteAccess.Configuration;
using NetSuiteAccess.Services.Common;
using NetSuiteAccess.Services.Items;
using NetSuiteAccess.Services.Orders;
using NetSuiteAccess.Throttling;

namespace NetSuiteAccess
{
	public interface INetSuiteFactory
	{
		INetSuiteOrdersService CreateOrdersService( NetSuiteConfig config );
		INetSuiteItemsService CreateItemsService( NetSuiteConfig config );
		INetSuiteCommonService CreateCommonService( NetSuiteConfig config );
	}
}