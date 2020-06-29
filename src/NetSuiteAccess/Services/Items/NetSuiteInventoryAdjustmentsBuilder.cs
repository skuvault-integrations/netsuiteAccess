using System;
using Netco.Logging;
using NetSuiteAccess.Models;
using NetSuiteAccess.Services.Soap;
using NetSuiteSoapWS;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NetSuiteAccess.Services.Items
{
	public class NetSuiteInventoryAdjustmentsBuilder
	{
		private readonly NetSuiteSoapService _service;
		private readonly NetSuiteLocation _location;
		private readonly NetSuitePushInventoryModeEnum _pushInventoryModeEnum;
		private readonly CancellationToken _token;
		private readonly Mark _mark;

		public List< InventoryAdjustmentInventory > InventoryAdjustments { get; }

		public NetSuiteInventoryAdjustmentsBuilder( NetSuiteSoapService service, NetSuiteLocation location, NetSuitePushInventoryModeEnum pushInventoryModeEnum, CancellationToken token, Mark mark )
		{
			_service = service;
			_location = location;
			_pushInventoryModeEnum = pushInventoryModeEnum;
			_token = token;
			_mark = mark;
			InventoryAdjustments = new List< InventoryAdjustmentInventory >();
		}

		public async System.Threading.Tasks.Task AddItemInventoryAdjustments( InventoryItem item, NetSuiteItemQuantity itemQuantity )
		{
			if ( item.useBins && this._location.UseBins )
			{ 
				if ( _pushInventoryModeEnum == NetSuitePushInventoryModeEnum.ItemsInBins || _pushInventoryModeEnum == NetSuitePushInventoryModeEnum.Both )
				{
					this.AddItemAdjustmentForBins( item, FilterBinQuantitiesByLocation( itemQuantity.BinQuantities ) );	
				}
			} else
			{
				if ( _pushInventoryModeEnum == NetSuitePushInventoryModeEnum.ItemsNotInBins || _pushInventoryModeEnum == NetSuitePushInventoryModeEnum.Both )
				{
					await this.AddItemAdjustment( item, itemQuantity.AvailableQuantity );
				}
			}
		}

		private IDictionary< string, int > FilterBinQuantitiesByLocation( IEnumerable< NetSuiteBinQuantity > binQuantities )
		{
			return binQuantities.Where( x => x.LocationName.Equals( this._location.Name, StringComparison.InvariantCultureIgnoreCase ) )
				.GroupBy( x => x.BinNumber.ToUpperInvariant(), x => x.Quantity )
				.ToDictionary( x => x.Key, x => x.Sum() );
		}

		private void AddItemAdjustmentForBins( InventoryItem item, IDictionary< string, int > incomingBinQuantities )
		{
			if ( !incomingBinQuantities.Any() )
				return;

			var binsInLocation = item.binNumberList?.binNumber?
				.Where( b => b.location == this._location.Id.ToString() )
				?? new List< InventoryItemBinNumber >();

			foreach ( var bin in binsInLocation )
			{
				var binName = bin.binNumber.name.ToUpperInvariant();
				if ( !incomingBinQuantities.ContainsKey( binName ) ) 
					continue;

				int existingBinQuantity;
				int.TryParse( bin.onHand, out existingBinQuantity );
				var adjustQuantityBy = incomingBinQuantities[ binName ] - existingBinQuantity;

				if ( adjustQuantityBy == 0 )
					continue;

				this.InventoryAdjustments.Add( new InventoryAdjustmentInventory()
				{
					adjustQtyBy = adjustQuantityBy, 
					item = new RecordRef { internalId = item.internalId }, 
					location = new RecordRef { internalId = _location.Id.ToString() }, 
					adjustQtyBySpecified = true,
					inventoryDetail = new InventoryDetail
					{
						inventoryAssignmentList = new InventoryAssignmentList
						{
							inventoryAssignment = new []
							{ 
								new InventoryAssignment
								{
									binNumber = new RecordRef { internalId = bin.binNumber.internalId },
									quantity = adjustQuantityBy,
									quantitySpecified = true
								}
							}
						}
					}
				} );
			}
		}

		private async System.Threading.Tasks.Task AddItemAdjustment( InventoryItem item, int incomingItemQuantity )
		{ 
			var itemInventory = await this._service.GetItemInventoryAsync( item, this._token, this._mark ).ConfigureAwait( false );

			var locationInventory = itemInventory?.FirstOrDefault( i => i.locationId.internalId.Equals( this._location.Id.ToString() ) );

			if ( locationInventory == null )
				return;

			var adjustQuantityBy = incomingItemQuantity - ( int )locationInventory.quantityOnHand;

			if ( adjustQuantityBy == 0 )
				return;

			this.InventoryAdjustments.Add( new InventoryAdjustmentInventory()
			{
				adjustQtyBy = adjustQuantityBy, 
				adjustQtyBySpecified = true,
				item = new RecordRef { internalId = item.internalId }, 
				location = new RecordRef { internalId = _location.Id.ToString() }
			} );
		}
	}
}
