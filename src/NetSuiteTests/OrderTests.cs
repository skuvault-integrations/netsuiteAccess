using FluentAssertions;
using NetSuiteAccess;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Orders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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

			await this._ordersService.CreatePurchaseOrderAsync( purchaseOrder, "SkuVault", CancellationToken.None );

			var purchaseOrders = await this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMinutes( -5 ), DateTime.UtcNow, CancellationToken.None );
			purchaseOrders.FirstOrDefault( p => p.DocNumber.Equals( docNumber ) ).Should().NotBeNull();
		}
	
		[ Test ]
		public void GetAllPurchaseOrders()
		{
			var allPurchaseOrders = this._ordersService.GetAllPurchaseOrdersAsync( CancellationToken.None ).Result;
			allPurchaseOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public async Task UpdatePurchaseOrder()
		{
			var orderInternalId = "14702";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );

			purchaseOrder.PrivateNote = "Test" + random.Next( 1, 1000 );
			purchaseOrder.SupplierName = "American Express";
			var newItems = new List< NetSuitePurchaseOrderItem >();
			newItems.AddRange( purchaseOrder.Items );
			newItems.Add( new NetSuitePurchaseOrderItem()
			{
				Sku = "NS-testsku18",
				Quantity = random.Next( 1, 100 ),
				UnitPrice = random.Next( 1, 100 )
			} );
			purchaseOrder.Items = newItems.ToArray();

			await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );

			var updatedPurchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			updatedPurchaseOrder.PrivateNote.Should().Be( purchaseOrder.PrivateNote );
			updatedPurchaseOrder.Items.Count().Should().Be( purchaseOrder.Items.Count() );
		}

		private NetSuitePurchaseOrder GetOrderByIdAsync( string internalId )
		{
			return this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None )
								.Result
								.FirstOrDefault( o => o.Id.Equals( internalId ) );
		}
	}
}