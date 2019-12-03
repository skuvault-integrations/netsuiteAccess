using NetSuiteAccess.Configuration;
using NetSuiteAccess.Services.Orders;
using NetSuiteAccess.Throttling;

namespace NetSuiteAccess
{
	public class NetSuiteFactory : INetSuiteFactory
	{
		public INetSuiteOrdersService CreateOrdersService( NetSuiteConfig config )
		{
			return new NetSuiteOrdersService( config );
		}
	}
}