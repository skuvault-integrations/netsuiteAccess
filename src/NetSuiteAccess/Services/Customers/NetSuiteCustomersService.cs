using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;

namespace NetSuiteAccess.Services.Customers
{
	public class NetSuiteCustomersService : BaseService, INetSuiteCustomersService
	{
		public NetSuiteCustomersService( NetSuiteConfig config ) : base( config )
		{ }

		public Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( long customerId, CancellationToken token )
		{
			return base.GetAsync< NetSuiteCustomer >( new GetCustomerInfoCommand( base.Config, customerId ), token );
		}
	}
}