using NetSuiteAccess.Configuration;
using NetSuiteAccess.Services.Common;
using NetSuiteAccess.Services.Items;
using NetSuiteAccess.Services.Orders;

namespace NetSuiteAccess
{
	public class NetSuiteFactory : INetSuiteFactory
	{
		public INetSuiteCommonService CreateCommonService( NetSuiteConfig config )
		{
			return new NetSuiteCommonService( config );
		}

		public INetSuiteItemsService CreateItemsService( NetSuiteConfig config )
		{
			return new NetSuiteItemsService( config );
		}

		public INetSuiteOrdersService CreateOrdersService( NetSuiteConfig config )
		{
			return new NetSuiteOrdersService( config );
		}
	}
}