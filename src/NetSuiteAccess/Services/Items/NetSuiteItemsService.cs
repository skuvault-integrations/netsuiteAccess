using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Services.Soap;
using NetSuiteSoapWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		///	Creates inventory adjustment document in NetSuite
		/// </summary>
		/// <param name="accountInternalId">Account</param>
		/// <param name="warehouseName">Warehouse name (location)</param>
		/// <param name="sku">Sku (item display name)</param>
		/// <param name="quantity">Quantity</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task UpdateItemQuantityBySkuAsync( int accountId, string warehouseName, string sku, int quantity, CancellationToken token )
		{
			if ( string.IsNullOrWhiteSpace( sku ) || string.IsNullOrWhiteSpace( warehouseName ) )
				return;

			var inventoryAdjustment = new Dictionary< string, int >
			{
				{ sku, quantity }
			};

			await this.UpdateSkusQuantitiesAsync( accountId, warehouseName, inventoryAdjustment, token ).ConfigureAwait( false );
		}

		/// <summary>
		///	Creates inventory adjustment document inside NetSuite
		/// </summary>
		/// <param name="accountInternalId">Account</param>
		/// <param name="warehouseName">Warehouse name (location)</param>
		/// <param name="skuQuantities">Sku (item display name)</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task UpdateSkusQuantitiesAsync( int accountId, string warehouseName, Dictionary< string, int > skuQuantities, CancellationToken token )
		{
			if ( string.IsNullOrWhiteSpace( warehouseName ) )
				return;

			var inventoryAdjustment = new List< InventoryAdjustmentInventory >();

			foreach( var skuQuantity in skuQuantities )
			{
				var item = await this._service.GetItemBySkuAsync( skuQuantity.Key, token ).ConfigureAwait( false );

				if ( item == null )
					continue;

				// otherwise we have to specify bin in the location
				if ( item.useBins )
					continue;

				var itemInventory = await this._service.GetItemInventoryAsync( item, token ).ConfigureAwait( false );

				if ( itemInventory == null )
					continue;

				var warehouseInventory = itemInventory.Where( i => i.locationId.name.ToLower().Equals( warehouseName.ToLower() ) ).FirstOrDefault();

				if ( warehouseInventory == null )
					continue;

				int adjustQuantityBy = skuQuantity.Value - (int)warehouseInventory.quantityOnHand;

				if ( adjustQuantityBy == 0 )
					continue;

				inventoryAdjustment.Add( new InventoryAdjustmentInventory()
				{
					adjustQtyBy = adjustQuantityBy, 
					item = new RecordRef() { internalId = item.internalId }, 
					location = warehouseInventory.locationId, 
					adjustQtyBySpecified = true, 
				} );
			}

			if ( inventoryAdjustment.Count > 0 )
			{
				await this._service.AdjustInventory( accountId, inventoryAdjustment.ToArray(), token ).ConfigureAwait( false );
			}
		}

		/// <summary>
		///	Find item by sku and get it's hand on quantity in specified warehouse
		/// </summary>
		/// <param name="sku">Sku (item displayName)</param>
		/// <param name="warehouseName">Warehouse (location)</param>
		/// <param name="token">Cancellation token</param>
		/// <returns></returns>
		public async Task< int > GetSkuQuantity( string sku, string warehouseName, CancellationToken token )
		{
			var item = await this._service.GetItemBySkuAsync( sku, token ).ConfigureAwait( false );

			if ( item == null )
				throw new NetSuiteItemNotFoundException( sku );

			var itemInventory = await this._service.GetItemInventoryAsync( item, token ).ConfigureAwait( false );
			var warehouseInventory = itemInventory.Where( i => i.locationId.name.ToLower().Equals( warehouseName.ToLower() ) ).FirstOrDefault();

			if ( warehouseInventory == null )
				return 0;

			return (int)warehouseInventory.quantityOnHand;
		}
	}
}