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

		public async Task< IEnumerable< NetSuitePurchaseOrder > > GetPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			var purchaseOrders = new List< NetSuitePurchaseOrder >();
			var command = new GetModifiedPurchaseOrdersCommand( base.Config, startDateUtc, endDateUtc );
			var ordersIds = await base.GetEntitiesIds( command, Config.OrdersPageSize, token ).ConfigureAwait( false );

			foreach( var orderId in ordersIds )
			{
				var purchaseOrder = await base.GetAsync< PurchaseOrder >( new GetPurchaseOrderCommand( this.Config, orderId ), token ).ConfigureAwait( false );
				purchaseOrders.Add( purchaseOrder.ToSVPurchaseOrder() );
			}

			return purchaseOrders.ToArray();
		}

		public async Task< IEnumerable< NetSuiteSalesOrder > > GetSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			var orders = new List< NetSuiteSalesOrder >();
			var command = new GetModifiedSalesOrdersCommand( base.Config, startDateUtc, endDateUtc );
			var ordersIds = await base.GetEntitiesIds( command, Config.OrdersPageSize, token ).ConfigureAwait( false );

			foreach( var orderId in ordersIds )
			{
				var order = await base.GetAsync< SalesOrder >( new GetSalesOrderCommand( this.Config, orderId ), token ).ConfigureAwait( false );
				orders.Add( order.ToSVSalesOrder() );
			}

			return orders.ToArray();
		}
	}
}