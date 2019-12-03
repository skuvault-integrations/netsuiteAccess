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
		public void GetModifiedOrders()
		{
			var orders = this._ordersService.GetOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			orders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedOrdersByPage()
		{
			Config.OrdersPageSize = 1;
			var orders = this._ordersService.GetOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			orders.Count().Should().BeGreaterThan( 0 );
		}
	}
}