using CuttingEdge.Conditions;
using Netco.Logging;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Soap;
using NetSuiteSoapWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NetSuiteAccess.Services.Items
{
	public class NetSuiteItemNotFoundException : Exception
	{
		private string _sku;

		public NetSuiteItemNotFoundException( string sku ) : base( string.Format( "Item with display name {0} is not found!", sku ) )
		{
			this._sku = sku;
		}
	}

	public class NetSuiteItemsService : INetSuiteItemsService
	{
		/// <summary>
		///	Extra logging information
		/// </summary>
		public Func< string > AdditionalLogInfo
		{
			get { return this._service.AdditionalLogInfo ?? ( () => string.Empty ); }
			set => this._service.AdditionalLogInfo = value;
		}

		private NetSuiteConfig _config;
		private NetSuiteSoapService _service;

		public NetSuiteItemsService( NetSuiteConfig config )
		{
			Condition.Requires( config, "config" ).IsNotNull();

			this._config = config;
			this._service = new NetSuiteSoapService( this._config );
		}

		/// <summary>
		///	Creates inventory adjustment document in NetSuite.
		///	Requires Transactions -> Adjust Inventory role permission.
		/// </summary>
		/// <param name="accountId">Account</param>
		/// <param name="locationName">SV Warehouse name (location)</param>
		/// <param name="sku">Sku (item display name)</param>
		/// <param name="quantity">Quantity</param>
		/// <param name="pushInventoryModeEnum"></param>
		/// <param name="token">Cancellation token</param>
		/// <param name="mark"></param>
		/// <param name="binName">Optional, only when update quantity in bins</param>
		/// <returns></returns>
		public async Task UpdateItemQuantityBySkuAsync( int accountId, string locationName, string sku, int quantity, NetSuitePushInventoryModeEnum pushInventoryModeEnum, CancellationToken token, Mark mark, string binName = "" )
		{
			if ( string.IsNullOrWhiteSpace( sku ) || string.IsNullOrWhiteSpace( locationName ) )
				return;

			var binInventory = new Dictionary< string, int >
			{
				{ binName, quantity }
			};

			var inventoryAdjustment = new Dictionary< string, Dictionary< string, int > >
			{
				{ sku, binInventory }
			};

			await this.UpdateSkusQuantitiesAsync( accountId, locationName, inventoryAdjustment, pushInventoryModeEnum, token, mark ).ConfigureAwait( false );
		}

		/// <summary>
		///	Creates inventory adjustment document inside NetSuite.
		///	Requires Transactions -> Adjust Inventory role permission.
		/// </summary>
		/// <param name="accountId">Account</param>
		/// <param name="locationName">SV Warehouse name (location)</param>
		/// <param name="skuBinQuantities">Bin/quantity for each sku</param>
		/// <param name="pushInventoryModeEnum">Determines whether to push to item locations in bins, not in bins, or both</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="mark">Mark to correlate logs</param>
		/// <returns></returns>
		public async Task UpdateSkusQuantitiesAsync( int accountId, string locationName, Dictionary< string, Dictionary < string, int > > skuBinQuantities, NetSuitePushInventoryModeEnum pushInventoryModeEnum, CancellationToken token, Mark mark )
		{
			if ( string.IsNullOrWhiteSpace( locationName ) )
				return;

			var location = await this._service.GetLocationByNameAsync( locationName, token, mark ).ConfigureAwait( false );

			if ( location == null )
			{
				throw new NetSuiteException( string.Format( "Warehouse {0} was not found! Inventory sync failed", locationName ) );
			}

			var inventoryAdjustmentsBuilder = new NetSuiteInventoryAdjustmentsBuilder( this._service, location, pushInventoryModeEnum, token, mark );			

			foreach( var skuBinQuantity in skuBinQuantities )
			{
				var sku = skuBinQuantity.Key;
				var item = await this._service.GetItemBySkuAsync( sku, token ).ConfigureAwait( false );

				if ( item == null )
					continue;

				// we can't specify quantity for parent items
				if ( item.matrixType == ItemMatrixType._parent && item.matrixTypeSpecified )
					continue;

				var binQuantities = skuBinQuantity.Value;
				await inventoryAdjustmentsBuilder.AddItemInventoryAdjustments( item, binQuantities );
			}

			var inventoryAdjustments = inventoryAdjustmentsBuilder.InventoryAdjustments.ToArray();
			if ( inventoryAdjustments.Length > 0 )
			{
				await this._service.AdjustInventoryAsync( accountId, inventoryAdjustments, token, mark ).ConfigureAwait( false );
			}
		}

		/// <summary>
		///	Find item by sku and get it's hand on quantity in specified location
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="sku">Sku (item displayName)</param>
		/// <param name="locationName">SV Warehouse (location)</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async Task< int > GetItemQuantityAsync( string sku, string locationName, CancellationToken token )
		{
			var mark = Mark.CreateNew();
			var item = await this._service.GetItemBySkuAsync( sku, token ).ConfigureAwait( false );

			if ( item == null )
				throw new NetSuiteItemNotFoundException( sku );

			var itemInventory = await this._service.GetItemInventoryAsync( item, token, mark ).ConfigureAwait( false );
			var locationInventory = itemInventory.FirstOrDefault(i => i.locationId.name.ToLower().Equals( locationName.ToLower() ));

			if ( locationInventory == null )
				return 0;

			return (int)locationInventory.quantityOnHand;
		}

		/// <summary>
		///	Lists all items that were created or updated after specified date
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="startDateUtc"></param>
		/// <param name="includeUpdated"></param>
		/// <param name="token">Cancellation token</param>
		/// <param name="mark">Mark to correlate logs</param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteItem > > GetItemsCreatedUpdatedAfterAsync( DateTime startDateUtc, bool includeUpdated, CancellationToken token, Mark mark )
		{
			var items = new List< NetSuiteItem >();
			var createdItems = this.ToNetSuiteItems( await this._service.GetItemsCreatedAfterAsync( startDateUtc, token ).ConfigureAwait( false ) );
			items.AddRange( createdItems );

			if ( includeUpdated )
			{
				var updatedItems = this.ToNetSuiteItems( await this._service.GetItemsModifiedAfterAsync( startDateUtc, token, mark ).ConfigureAwait( false ) );
				var updatedItemsWithoutDublicates = updatedItems.Where( i => !createdItems.Where( cr => !string.IsNullOrWhiteSpace( cr.Sku ) ).Any( cr => cr.Sku.Equals( i.Sku ) ) ).ToArray();
				items.AddRange( updatedItemsWithoutDublicates );
			}

			return items.ToArray();
		}

		private IEnumerable< NetSuiteItem > ToNetSuiteItems( IEnumerable< Record > records )
		{
			var items = new List< NetSuiteItem >();
			items.AddRange( records.OfType< InventoryItem >().Select( r => r.ToSVItem() ) );
			items.AddRange( records.OfType< SerializedInventoryItem >().Select( r => r.ToSVItem() ) );
			items.AddRange( records.OfType< LotNumberedInventoryItem >().Select( r => r.ToSVItem() ) );

			return items;
		}
	}
}