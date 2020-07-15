using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Netco.Logging;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Soap;

namespace NetSuiteAccess.Services.Common
{
	public class NetSuiteCommonService : INetSuiteCommonService
	{
		private NetSuiteSoapService _soapService;

		public NetSuiteCommonService( NetSuiteConfig config )
		{
			_soapService = new NetSuiteSoapService( config );
		}

		public Task< IEnumerable< NetSuiteAccount > > GetAccountsAsync( CancellationToken token )
		{
			return _soapService.ListAccountsAsync( token );
		}

		/// <summary>
		///	Get all inventory locations.
		///	Requires Lists -> Locations role permission.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< IEnumerable< NetSuiteLocation > > GetLocationsAsync( CancellationToken token )
		{
			return _soapService.ListLocationsAsync( token );
		}

		public Task< NetSuiteLocation > GetLocationByNameAsync( string locationName, CancellationToken token, Mark mark )
		{
			return _soapService.GetLocationByNameAsync( locationName, token, mark );
		}
	}
}