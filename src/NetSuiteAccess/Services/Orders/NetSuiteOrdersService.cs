using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;
using NetSuiteAccess.Services.Common;
using NetSuiteAccess.Services.Customers;
using NetSuiteAccess.Services.Soap;
using NetSuiteAccess.Shared;

namespace NetSuiteAccess.Services.Orders
{
	public sealed class NetSuiteOrdersService : BaseService, INetSuiteOrdersService
	{
		private INetSuiteCustomersService _customersService;
		private INetSuiteCommonService _commonService;
		private NetSuiteSoapService _soapService;

		public NetSuiteOrdersService( NetSuiteConfig config ) : base( config )
		{
			this._customersService = new NetSuiteCustomersService( config );
			this._commonService = new NetSuiteCommonService( config );
			this._soapService = new NetSuiteSoapService( config );
		}

		/// <summary>
		///	Create purchase order.
		///	Requires Transactions -> Purchase Order role permission. Level - Create or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="locationName"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task CreatePurchaseOrderAsync( NetSuitePurchaseOrder order, string locationName, CancellationToken token )
		{
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );

			await this._soapService.CreatePurchaseOrderAsync( order, location.Id, token ).ConfigureAwait( false );
		}

		/// <summary>
		///	Update purchase order.
		///	Requires Transactions -> Purchase Order role permission. Level - Edit or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="none"></param>
		/// <returns></returns>
		public Task UpdatePurchaseOrderAsync( NetSuitePurchaseOrder order, CancellationToken token )
		{
			return this._soapService.UpdatePurchaseOrderAsync( order, token );
		}

		/// <summary>
		///	Get all purchase orders
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< IEnumerable< NetSuitePurchaseOrder > > GetAllPurchaseOrdersAsync( CancellationToken token )
		{
			return this._soapService.GetAllPurchaseOrdersAsync( token );
		}

		/// <summary>
		///	Lists purchase orders that were changed
		/// </summary>
		/// <param name="startDateUtc"></param>
		/// <param name="endDateUtc"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuitePurchaseOrder > > GetPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			var purchaseOrders = new List< NetSuitePurchaseOrder >();
			var command = new GetModifiedPurchaseOrdersCommand( base.Config, startDateUtc, endDateUtc );
			var ordersIds = await base.GetEntitiesIds( command, Config.OrdersPageSize, token ).ConfigureAwait( false );

			foreach( var orderId in ordersIds )
			{
				try
				{
					var purchaseOrder = await base.GetAsync< PurchaseOrder >( new GetPurchaseOrderCommand( this.Config, orderId ), token ).ConfigureAwait( false );
					purchaseOrders.Add( purchaseOrder.ToSVPurchaseOrder() );
				}
				catch( NetSuiteResourceAccessException ex )
				{
					// ignore order with issue, log and continue
					NetSuiteLogger.LogTrace( ex, string.Format( "Skipped purchase order {0} with internal error", orderId ) );
				}
			}

			return purchaseOrders.ToArray();
		}

		/// <summary>
		///  Lists sales orders that were changed.
		///  Requires Transactions -> Sales Order permission.
		/// </summary>
		/// <param name="startDateUtc"></param>
		/// <param name="endDateUtc"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteSalesOrder > > GetSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken token )
		{
			var orders = new List< NetSuiteSalesOrder >();
			var command = new GetModifiedSalesOrdersCommand( base.Config, startDateUtc, endDateUtc );
			var ordersIds = await base.GetEntitiesIds( command, Config.OrdersPageSize, token ).ConfigureAwait( false );

			foreach( var orderId in ordersIds )
			{
				try
				{
					var order = await base.GetAsync< SalesOrder >( new GetSalesOrderCommand( this.Config, orderId ), token ).ConfigureAwait( false );
					var svOrder = order.ToSVSalesOrder();
					await FillCustomerData( svOrder, token ).ConfigureAwait( false );
					orders.Add( svOrder );
				}
				catch( NetSuiteResourceAccessException ex )
				{
					// ignore order with issue, log and continue
					NetSuiteLogger.LogTrace( ex, string.Format( "Skipped sales order {0} with internal error", orderId ) );
				}
			}

			return orders.ToArray();
		}

		/// <summary>
		///	Fill sales order's customer property
		///	Requires Lists -> Customers permission.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		private async Task FillCustomerData( NetSuiteSalesOrder order, CancellationToken token )
		{
			if ( order.Customer != null )
			{
				var customerInfo = await this._customersService.GetCustomerInfoByIdAsync( order.Customer.Id, token ).ConfigureAwait( false );

				if ( customerInfo != null )
				{
					order.Customer = customerInfo;
				}
			}
		}

		/// <summary>
		///	Creates sales order in NetSuite
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async Task CreateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token )
		{
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
			{
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );
			}

			var customer = await this._customersService.GetCustomerInfoByEmailAsync( order.Customer.Email, token ).ConfigureAwait( false );

			if ( customer == null )
			{
				NetSuiteLogger.LogTrace( string.Format( "Can't create sales order in NetSuite! Customer with email {0} was not found!", order.Customer.Email ) );
				return;
			}

			await this._soapService.CreateSalesOrderAsync( order, location.Id, customer.Id, token ).ConfigureAwait( false );
		}

		/// <summary>
		///	Updates existing sales order in NetSuite
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="">Cancellation token</param>
		/// <returns></returns>
		public async Task UpdateSalesOrderAsync( NetSuiteSalesOrder order, string locationName, CancellationToken token )
		{
			var location = await this.GetLocationByNameAsync( locationName, token ).ConfigureAwait( false );

			if ( location == null )
			{
				throw new NetSuiteException( string.Format( "Location with name {0} is not found in NetSuite!", locationName ) );
			}

			var customer = await this._customersService.GetCustomerInfoByEmailAsync( order.Customer.Email, token ).ConfigureAwait( false );

			if ( customer == null )
			{
				NetSuiteLogger.LogTrace( string.Format( "Can't update sales order in NetSuite! Customer with email {0} was not found!", order.Customer.Email ) );
				return;
			}

			await this._soapService.UpdateSalesOrderAsync( order, location.Id, customer.Id, token ).ConfigureAwait( false );
		}

		private async Task< NetSuiteLocation > GetLocationByNameAsync( string locationName, CancellationToken token )
		{
			var locations = await this._commonService.GetLocationsAsync( token ).ConfigureAwait( false );
			return locations.FirstOrDefault( l => l.Name.ToLower().Equals( locationName.ToLower() ) );
		}
	}
}