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

		/// <summary>
		///	Get customer information by internal id.
		///	Requires Lists -> Customers role permission.
		/// </summary>
		/// <param name="customerId"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( long customerId, CancellationToken token )
		{
			return base.GetAsync< NetSuiteCustomer >( new GetCustomerInfoCommand( base.Config, customerId ), token );
		}
	}
}