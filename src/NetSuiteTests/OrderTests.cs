using FluentAssertions;
using NetSuiteAccess;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Orders;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

		[ Test ]
		public async Task CreatePurchaseOrder()
		{
			var docNumber = "PO_12347";
			var purchaseOrder = new NetSuitePurchaseOrder()
			{
				 DocNumber = docNumber,
				 CreatedDateUtc = DateTime.UtcNow,
				 Items = new NetSuitePurchaseOrderItem[]
				 {
					 new NetSuitePurchaseOrderItem()
					 {
						Sku = "NS-testsku12",
						Quantity = 12
					 },
					 new NetSuitePurchaseOrderItem()
					 {
						Sku = "NS-testsku17",
						Quantity = 35
					 }
				 }, 
				 SupplierName = "Samsung"
			};

			await this._ordersService.CreatePurchaseOrder( purchaseOrder, "SkuVault", CancellationToken.None );

			var purchaseOrders = await this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMinutes( -5 ), DateTime.UtcNow, CancellationToken.None );
			purchaseOrders.FirstOrDefault( p => p.DocNumber.Equals( docNumber ) ).Should().NotBeNull();
		}
	}
}