using NetSuiteAccess.Configuration;
using NetSuiteAccess.Services.Orders;
using NetSuiteAccess.Throttling;

namespace NetSuiteAccess
{
	public interface INetSuiteFactory
	{
		INetSuiteOrdersService CreateOrdersService( NetSuiteConfig config );
	}
}