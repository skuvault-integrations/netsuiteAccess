using FluentAssertions;
using NetSuiteAccess;
using NetSuiteAccess.Exceptions;
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
		private string[] _testSkus = new string[] { "NS-testsku12", "NS-testsku17", "NS-testsku18", "NS-testsku22" };
		private string _locationName = "SkuVault";

		[ SetUp ]
		public void Init()
		{
			this._ordersService = new NetSuiteFactory().CreateOrdersService( this.Config );
		}

		[ Test ]
		public void GetModifiedSalesOrders()
		{
			var salesOrders = this._ordersService.GetSalesOrdersAsync( DateTime.UtcNow.AddMonths( -3 ), DateTime.UtcNow, CancellationToken.None ).Result;
			salesOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedSalesOrdersByPage()
		{
			Config.SearchRecordsPageSize = 5;
			var salesOrders = this._ordersService.GetSalesOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			salesOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedSalesOrdersWithFulfillments()
		{
			var salesOrders = this._ordersService.GetSalesOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None, true ).Result;
			
			salesOrders.Count().Should().BeGreaterThan( 0 );
			salesOrders.Where( s => s.Fulfillments.Count() > 0 ).Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedPurchaseOrders()
		{
			var purchaseOrders = this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddDays( -14 ), DateTime.UtcNow, CancellationToken.None ).Result;
			purchaseOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetModifiedPurchaseOrdersByPage()
		{
			Config.SearchRecordsPageSize = 5;
			var purchaseOrders = this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMonths( -1 ), DateTime.UtcNow, CancellationToken.None ).Result;
			purchaseOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GivenHugeDefaultPurchaseOrdersPageSize_WhenGetPurchaseOrdersAsyncIsCalled_ThenExceptionIsNotExpected()
		{
			Config.SearchPurchaseOrdersPageSize = 200;

			Assert.DoesNotThrow( () => this._ordersService.GetPurchaseOrdersAsync(
				DateTime.UtcNow.AddDays( -14 ), 
				DateTime.UtcNow, 
				CancellationToken.None 
			).Wait() );
		}

		[ Test ]
		public async Task CreatePurchaseOrder()
		{
			var docNumber = "PO_" + Guid.NewGuid().ToString();
			var purchaseOrder = new NetSuitePurchaseOrder()
			{
				 DocNumber = docNumber,
				 CreatedDateUtc = DateTime.UtcNow,
				 Items = new NetSuitePurchaseOrderItem[]
				 {
					 new NetSuitePurchaseOrderItem()
					 {
						Sku = _testSkus[0],
						Quantity = 12
					 },
					 new NetSuitePurchaseOrderItem()
					 {
						Sku = _testSkus[1],
						Quantity = 35
					 }
				 }, 
				 SupplierName = "Samsung"
			};

			await this._ordersService.CreatePurchaseOrderAsync( purchaseOrder, _locationName, CancellationToken.None );

			var purchaseOrders = await this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMinutes( -5 ), DateTime.UtcNow, CancellationToken.None );
			purchaseOrders.FirstOrDefault( p => p.DocNumber.Equals( docNumber ) ).Should().NotBeNull();
		}

		[ Test ]
		// This test can fail if someone creates a product with the sku listed in the test.
		public async Task CreatePurchaseOrderWhereItemsNotExist()
		{
			var docNumber = "PO_" + Guid.NewGuid().ToString();
			var purchaseOrder = new NetSuitePurchaseOrder()
			{
				 DocNumber = docNumber,
				 CreatedDateUtc = DateTime.UtcNow,
				 Items = new NetSuitePurchaseOrderItem[]
				 {
					 new NetSuitePurchaseOrderItem()
					 {
						Sku = "DoesNotExist123456",
						Quantity = 12
					 }
				 }, 
				 SupplierName = "Samsung"
			};

			await this._ordersService.CreatePurchaseOrderAsync( purchaseOrder, _locationName, CancellationToken.None );

			var purchaseOrders = await this._ordersService.GetPurchaseOrdersAsync( DateTime.UtcNow.AddMinutes( -5 ), DateTime.UtcNow, CancellationToken.None );
			purchaseOrders.FirstOrDefault( p => p.DocNumber.Equals( docNumber ) ).Should().BeNull();
		}
	
		[ Test ]
		public void GetAllPurchaseOrders()
		{
			var allPurchaseOrders = this._ordersService.GetAllPurchaseOrdersAsync( CancellationToken.None ).Result;
			allPurchaseOrders.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		// Updates will only be processed for POs with "Pending Receipt" status.
		// Will have to create new PO to test this if this status has changed.
		public async Task UpdatePurchaseOrder()
		{
			var orderInternalId = "56702";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );

			purchaseOrder.PrivateNote = "Test" + random.Next( 1, 1000 );
			purchaseOrder.SupplierName = "American Express";
			var newItems = new List< NetSuitePurchaseOrderItem >();
			newItems.AddRange( purchaseOrder.Items );
			newItems.Add( new NetSuitePurchaseOrderItem()
			{
				Sku = _testSkus[2],
				Quantity = random.Next( 1, 100 ),
				UnitPrice = random.Next( 1, 100 )
			} );
			purchaseOrder.Items = newItems.ToArray();

			await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );

			var updatedPurchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			updatedPurchaseOrder.PrivateNote.Should().Be( purchaseOrder.PrivateNote );
			updatedPurchaseOrder.Items.Count().Should().Be( purchaseOrder.Items.Count() );
		}

		[ Test ]
		// This test can fail if the PO has not been modified in the last 2 months.
		// If needed, updating the memo on the PO will update the LastModified date of the PO.
		public void AddReceivedQuantityToPurchaseOrderWithoutItemReceipt()
		{
			var orderInternalId = "26006";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			purchaseOrder.Items.First().ReceivedQuantity = random.Next( 1, purchaseOrder.Items.First().Quantity - 1 );

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );
			} );
		}

		[ Test ]
		// This test can fail if the PO has not been modified in the last 2 months.
		// If needed, updating the memo on the PO will update the LastModified date of the PO.
		public void AddReceivedQuantityToPurchaseOrderWithItemReceipt()
		{
			var orderInternalId = "26100";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			purchaseOrder.Items.First().ReceivedQuantity = random.Next( 1, purchaseOrder.Items.First().Quantity - 1 );

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );
			} );
		}

		[ Test ]
		// This test can fail if the PO has not been modified in the last 2 months.
		// If needed, updating the memo on the PO will update the LastModified date of the PO.
		public void AddAllReceivedQuantityToPurchaseOrderWithItemReceipt()
		{
			var orderInternalId = "26100";
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			purchaseOrder.Items.First().ReceivedQuantity = purchaseOrder.Items.First().Quantity;

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );
			} );
		}

		[ Test ]
		// This test can fail if the PO has not been modified in the last 2 months.
		// If needed, updating the memo on the PO will update the LastModified date of the PO.
		public void AddReceivedQuantityToPurchaseOrderCreatedFrom()
		{
			var orderInternalId = "56502";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			purchaseOrder.Items.First().ReceivedQuantity = random.Next( 1, purchaseOrder.Items.First().Quantity - 1 );

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );
			} );
		}

		[ Test ]
		// This test can fail if the PO has not been modified in the last 2 months.
		// If needed, updating the memo on the PO will update the LastModified date of the PO.
		public void SetZeroReceivedQuantityToPurchaseOrderWithItemReceiptAndNotPendingReceiptStatus()
		{
			var orderInternalId = "26005";
			var random = new Random();
			var purchaseOrder = this.GetOrderByIdAsync( orderInternalId );
			purchaseOrder.Items.First().ReceivedQuantity = 0;

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._ordersService.UpdatePurchaseOrderAsync( purchaseOrder, CancellationToken.None );
			} );
		}

		private NetSuitePurchaseOrder GetOrderByIdAsync( string internalId )
		{
			var pos = this._ordersService.GetPurchaseOrdersAsync(DateTime.UtcNow.AddMonths(-2), DateTime.UtcNow,
				CancellationToken.None).Result;
			var searchedPo = pos.FirstOrDefault( o => o.Id.Equals( internalId ) );
			return searchedPo;
		}

		[ Test ]
		// TODO: Failing test. Related to missing a value for "amountPaid" param. Discovered on GUARD-2073
		public async Task CreateSalesOrder()
		{
			var docNumber = "SO_" + Guid.NewGuid().ToString();
			var order = this.GenerateSalesOrder( docNumber );

			await this._ordersService.CreateSalesOrderAsync( order, _locationName, CancellationToken.None );

			var createdOrder = await GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			createdOrder.Should().NotBeNull();
		}

		[ Test ]
		public async Task GivenSalesOrderMadeByNewCustomer_WhenCreateSalesOrderIsCalled_ThenCreatedSalesOrderIsExpected()
		{
			var docNumber = "SO_" + Guid.NewGuid().ToString();
			var order = this.GenerateSalesOrder( docNumber );
			order.Customer.Email = "ivan.ivanov" + GenerateRandomPrefix() + "@skuvault.com";
			order.Customer.FirstName = "Ivan";
			order.Customer.LastName = "Ivanov_" + GenerateRandomPrefix();

			await this._ordersService.CreateSalesOrderAsync( order, _locationName, CancellationToken.None, true );

			var createdOrder = await GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			createdOrder.Should().NotBeNull();
		}

		[ Test ]
		public async Task GivenSalesOrderMadeByNewCustomer_WhenCreateSalesOrderIsCalledWithDisabledAutomaticCustomerCreationFeature_ThenExceptionIsExpected()
		{
			var docNumber = "SO_" + Guid.NewGuid().ToString();
			var order = this.GenerateSalesOrder( docNumber );

			order.Customer.Email = "ivan.ivanov" + GenerateRandomPrefix() + "@skuvault.com";
			order.Customer.FirstName = "Ivan";
			order.Customer.LastName = "Ivanov_" + GenerateRandomPrefix();

			await this._ordersService.CreateSalesOrderAsync( order, _locationName, CancellationToken.None, false );

			var createdOrder = await GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			createdOrder.Should().BeNull();
		}

		[ Test ]
		public async Task GivenSalesOrderMadeByNewCustomerWithSameNameInNetSuiteButDifferentEmail_WhenCreateSalesOrderIsCalled_ThenCreatedSalesOrderIsExpected()
		{
			var docNumber = "SO_" + Guid.NewGuid().ToString();
			var order = this.GenerateSalesOrder( docNumber );

			order.Customer.Email = "ivan.ivanov" + GenerateRandomPrefix() + "@skuvault.com";
			order.Customer.FirstName = "Ivan";
			order.Customer.LastName = "Ivanov";

			await this._ordersService.CreateSalesOrderAsync( order, _locationName, CancellationToken.None, true );

			var createdOrder = await GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			createdOrder.Should().NotBeNull();
		}

		[ Test ]
		// TODO: Failing test. Related to missing a value for "amountPaid" param. Discovered on GUARD-2073
		public async Task UpdateSalesOrder()
		{
			var docNumber = "SO_" + Guid.NewGuid().ToString();
			var order = this.GenerateSalesOrder( docNumber );
			await this._ordersService.CreateSalesOrderAsync( order, _locationName, CancellationToken.None );
			var createdOrder = await GetRecentlyModifiedSalesOrderByDocNumber( docNumber );

			var newItemQuantity = new Random().Next( 1, 100 );
			createdOrder.Items.First().Quantity = newItemQuantity;

			await this._ordersService.UpdateSalesOrderAsync( createdOrder, _locationName, CancellationToken.None );

			var updatedOrder = await this.GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			updatedOrder.Items.First().Quantity.Should().Be( newItemQuantity );
		}

		[ Test ]
		// TODO: Failing test. Related to missing a value for "amountPaid" param. Discovered on GUARD-2073
		public async Task UpdateExistingSalesOrder()
		{
			var docNumber = "698";
			var order = await this.GetRecentlyModifiedSalesOrderByDocNumber( docNumber, DateTime.UtcNow.AddMonths( -3 ) );
			var newQuantity = new Random().Next( 1, 100 );
			order.Items.First().Quantity = newQuantity;

			await this._ordersService.UpdateSalesOrderAsync( order, _locationName, CancellationToken.None );

			var updatedOrder = await this.GetRecentlyModifiedSalesOrderByDocNumber( docNumber );
			updatedOrder.Items.First().Quantity.Should().Be( newQuantity );
		}

		private NetSuiteSalesOrder GenerateSalesOrder( string docNumber )
		{
			return new NetSuiteSalesOrder()
			{
				DocNumber = docNumber, 
				Customer = new NetSuiteCustomer()
				{
					Email = "integrations@skuvault.com"
				},
				Items = new NetSuiteSalesOrderItem[]
				 {
					 new NetSuiteSalesOrderItem()
					 {
						Sku = _testSkus[ 3 ],
						Quantity = 12,
						UnitPrice = 5
					 }
				 }
			};
		}

		private async Task< NetSuiteSalesOrder > GetRecentlyModifiedSalesOrderByDocNumber( string docNumber, DateTime? startDate = null )
		{
			if ( startDate == null )
			{
				startDate = DateTime.UtcNow.AddHours( -1 );
			}
			var orders = await this._ordersService.GetSalesOrdersAsync( startDate.Value, DateTime.UtcNow, CancellationToken.None );
			return orders.FirstOrDefault( o => o.DocNumber.Equals( docNumber ) );
		}

		private string GenerateRandomPrefix()
		{
			return Guid.NewGuid().ToString().Substring( 0, 5 );
		}
	}
}