using NetSuiteAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Orders
{
	public interface INetSuiteOrdersService
	{
		Task< IEnumerable< NetSuiteOrder > > GetOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token );
	}
}