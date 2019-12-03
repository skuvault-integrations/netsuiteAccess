using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;

namespace NetSuiteAccess.Services.Orders
{
	public sealed class NetSuiteOrdersService : BaseService, INetSuiteOrdersService
	{
		public NetSuiteOrdersService( NetSuiteConfig config ) : base( config )
		{
		}

		public async Task< IEnumerable< NetSuiteOrder > > GetOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			var orders = new List< NetSuiteOrder >();
			var command = new GetModifiedSalesOrdersCommand( base.Config, startDateUtc, endDateUtc );
			var ordersIds = await base.GetEntitiesIds( command, Config.OrdersPageSize, token ).ConfigureAwait( false );

			foreach( var orderId in ordersIds )
			{
				var order = await base.GetAsync< Order >( new GetOrderCommand( this.Config, orderId ), token ).ConfigureAwait( false );
				orders.Add( order.ToSvOrder() );
			}

			return orders.ToArray();
		}
	}
}