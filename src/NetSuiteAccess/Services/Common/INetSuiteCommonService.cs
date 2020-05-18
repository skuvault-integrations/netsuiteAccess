using NetSuiteAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Common
{
	public interface INetSuiteCommonService
	{
		Task< IEnumerable< NetSuiteLocation > > GetLocationsAsync( CancellationToken token );
		Task< IEnumerable< NetSuiteAccount > > GetAccountsAsync( CancellationToken token );
		Task< NetSuiteLocation > GetLocationByNameAsync( string locationName, CancellationToken token );
	}
}