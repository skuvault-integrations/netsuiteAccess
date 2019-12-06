using FluentAssertions;
using NetSuiteAccess;
using NetSuiteAccess.Services.Orders;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class OrderTests : BaseTest
	{
		private INetSuiteOrdersService _ordersService;

		[ SetUp ]
		public void Init()
		{
			this._ordersService = new NetSuiteFactory().CreateOrdersService( this.Config );
		}

		[ Test ]
		public void GetModifiedSalesOrders()
		{
			var salesOrders = this._ordersService.GetSalesOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			salesOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedSalesOrdersByPage()
		{
			Config.OrdersPageSize = 1;
			var salesOrders = this._ordersService.GetSalesOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			salesOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedPurchaseOrders()
		{
			var purchaseOrders = this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			purchaseOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedPurchaseOrdersByPage()
		{
			Config.OrdersPageSize = 1;
			var purchaseOrders = this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			purchaseOrders.Count().Should().BeGreaterThan( 0 );
		}
	}
}