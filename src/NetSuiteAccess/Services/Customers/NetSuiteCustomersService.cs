using System.Collections.Generic;
using System.Linq;
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

		public async Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( string customerId, CancellationToken token )
		{
			var customers = await this.GetCustomersInfoByIdsAsync( new string[] { customerId }, token ).ConfigureAwait( false );

			return customers?.FirstOrDefault();
		}

		/// <summary>
		///	Get customers by theirs ids
		///	Requires Lists -> Customers role permission.
		/// </summary>
		/// <param name="customerId"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteCustomer > > GetCustomersInfoByIdsAsync( string[] customersIds, CancellationToken token )
		{
			var customers = await _soapService.GetCustomersByIdsAsync( customersIds, token ).ConfigureAwait( false );

			if ( customers != null && customers.Any() )
			{
				return customers.Select( c => c.ToSVCustomer() );
			}
			
			return null;
		}
	}
}