using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;
using NetSuiteAccess.Services.Soap;

namespace NetSuiteAccess.Services.Common
{
	public class NetSuiteCommonService : BaseService, INetSuiteCommonService
	{
		private NetSuiteSoapService _soapService;

		public NetSuiteCommonService( NetSuiteConfig config ) : base( config )
		{
			_soapService = new NetSuiteSoapService( config );
		}

		public Task< IEnumerable< NetSuiteAccount > > GetAccountsAsync( CancellationToken token )
		{
			return _soapService.ListAccounts( token );
		}

		public async Task< IEnumerable< NetSuiteLocation > > GetLocationsAsync( CancellationToken token )
		{
			var locations = new List< NetSuiteLocation >();
			var locationsIds = await base.GetEntitiesIds( new ListLocationsCommand( base.Config ), Config.LocationsPageSize, token ).ConfigureAwait( false );

			foreach( var locationId in locationsIds )
			{
				var location = await base.GetAsync< NetSuiteLocation >( new GetLocationCommand( base.Config, locationId ), token ).ConfigureAwait( false );
				locations.Add( location );
			}

			return locations.ToArray();
		}
	}
}