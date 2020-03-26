using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;
using NetSuiteAccess.Services.Soap;

namespace NetSuiteAccess.Services.Customers
{
	public class NetSuiteCustomersService : BaseService, INetSuiteCustomersService
	{
		private NetSuiteSoapService _soapService;

		public NetSuiteCustomersService( NetSuiteConfig config ) : base( config )
		{
			this._soapService = new NetSuiteSoapService( config );
		}

		public async Task< NetSuiteCustomer > GetCustomerInfoByEmailAsync( string email, CancellationToken token )
		{
			var customer = await this._soapService.GetCustomerByEmailAsync( email, token ).ConfigureAwait( false );

			return customer?.ToSVCustomer();
		}

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