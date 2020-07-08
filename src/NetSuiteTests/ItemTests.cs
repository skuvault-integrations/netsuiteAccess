using FluentAssertions;
using Netco.Logging;
using NetSuiteAccess.Services.Items;
using NetSuiteAccess.Services.Soap;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetSuiteSoapWS;
using Task = System.Threading.Tasks.Task;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class ItemTests : BaseTest
	{
		private INetSuiteItemsService _itemsService;
		private readonly RecordRef _locationSkuVaultNoBins = new RecordRef { name = "SkuVault", internalId = "6" };
		private readonly RecordRef _locationBostonBins = new RecordRef { name = "Boston", internalId = "1" };
		private const int AccountId = 54;

		[ SetUp ]
		public void Init()
		{
			this._itemsService = new NetSuiteItemsService( base.Config );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_ItemNotInBin()
		{
			int newQuantity = new Random().Next( 1, 100 );
			string testSku = "NS-testsku1";

			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, _locationSkuVaultNoBins.name, testSku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			int quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testSku, _locationSkuVaultNoBins.internalId, CancellationToken.None );
			quantityAfterUpdate.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_BinFeatureEnabledOnlyOnItemLevel()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "NS-testsku555";
			
			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, _locationSkuVaultNoBins.name, testsku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			int updatedQuantity = await this._itemsService.GetItemQuantityAsync( testsku, _locationSkuVaultNoBins.internalId, CancellationToken.None );
			updatedQuantity.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_BinFeatureEnabledOnlyOnLocationLevel()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testSku = "NS-testsku1";

			await _itemsService.UpdateItemQuantityBySkuAsync( AccountId, _locationBostonBins.name, testSku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			var quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testSku, _locationBostonBins.internalId, CancellationToken.None );
			quantityAfterUpdate.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_ModeSetToPushToNotInBinOnly_ItemAndLocationUsesBins()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testsku = "NS-testsku555";
			var initialQuantity = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );
			
			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, _locationBostonBins.name,  testsku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			var quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );
			quantityAfterUpdate.Should().Be( initialQuantity );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_ItemAndLocationUsesBins()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "GUARD528-test1";
			const string binNumber = "1004";
			var token = CancellationToken.None;

			var itemQuantity = new NetSuiteItemQuantity
			{
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( _locationBostonBins.name, binNumber, newQuantity )
				}
			};
			var skuLocationQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testsku, itemQuantity }
			};
			var binQuantityBefore = await GetItemBinQuantityAsync( testsku, _locationBostonBins.internalId, binNumber );
			var itemQuantityBefore = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, _locationBostonBins.name, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var binQuantityAfter = await GetItemBinQuantityAsync( testsku, _locationBostonBins.internalId, binNumber );;
			binQuantityAfter.Should().Be( newQuantity );
			var itemQuantityAfter = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );
			var quantityChange = binQuantityAfter - binQuantityBefore;
			itemQuantityAfter.Should().Be( itemQuantityBefore + quantityChange );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_UseBins_BinAssociatedWithItemButInAnotherLocation()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testSku = "GUARD528-test1";
			const string locationName = "San Francisco";
			const string locationId = "1";
			const string binNumber = "1004";	//in the Boston location
			var token = CancellationToken.None;

			var itemQuantity = new NetSuiteItemQuantity
			{
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( locationName, binNumber, newQuantity )
				}
			};
			var skuLocationQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testSku, itemQuantity }
			};
			var binQuantityBefore = await GetItemBinQuantityAsync( testSku, locationId, binNumber );
			var itemQuantityBefore = await this._itemsService.GetItemQuantityAsync( testSku, locationName, CancellationToken.None );

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var binQuantityAfter = await GetItemBinQuantityAsync( testSku, locationId, binNumber );;
			binQuantityAfter.Should().Be( binQuantityBefore );
			var itemQuantityAfter = await this._itemsService.GetItemQuantityAsync( testSku, locationName, CancellationToken.None );
			itemQuantityAfter.Should().Be( itemQuantityBefore );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_ItemAndLocationUsesBins_BinIsNotAssociatedWithItem()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "GUARD528-test1";
			const string binNumber = "1999";
			var token = CancellationToken.None;
			var itemQuantity = new NetSuiteItemQuantity
			{
				AvailableQuantity = 0,
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( _locationBostonBins.name, binNumber, newQuantity )
				}
			};
			var skuLocationQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testsku, itemQuantity }
			};
			var binQuantityBefore = await GetItemBinQuantityAsync( testsku, _locationBostonBins.internalId, binNumber );
			var itemQuantityBefore = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, _locationBostonBins.name, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var updatedBinQuantity = await GetItemBinQuantityAsync( testsku, _locationBostonBins.internalId, binNumber );;
			updatedBinQuantity.Should().Be( binQuantityBefore );
			var itemQuantityAfter = await this._itemsService.GetItemQuantityAsync( testsku, _locationBostonBins.internalId, CancellationToken.None );
			itemQuantityAfter.Should().Be( itemQuantityBefore );
		}

		private async Task< int > GetItemBinQuantityAsync( string sku, string locationId, string binNumber )
		{
			var inventoryItem = await new NetSuiteSoapService( this.Config ).GetItemBySkuAsync( sku, CancellationToken.None );;
			return int.Parse( inventoryItem.binNumberList?.binNumber?.FirstOrDefault(
				b => b.location == locationId && b.binNumber.name == binNumber )?.onHand ?? "0" );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_PushItemsThatUsesBinsAndItemThatDoesnt()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testSkuBin = "GUARD528-test1";
			const string binNumber = "1004";

			int newQuantityNoBin = new Random().Next( 1, 100 );
			const string testSkuNoBin = "NS-testsku99";

			var token = CancellationToken.None;
			var binQuantities = new NetSuiteItemQuantity
			{
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( _locationBostonBins.name, binNumber, newQuantity )
				}
			};
			var unBinnedQuantities = new NetSuiteItemQuantity
			{
				AvailableQuantity = newQuantityNoBin
			};
			var skuBinQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testSkuBin, binQuantities },
				{ testSkuNoBin, unBinnedQuantities }
			};

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, _locationBostonBins.name, skuBinQuantities, 
				NetSuiteInventoryBinsModeEnum.Both, token, Mark.Blank() );

			var updatedBinQuantity = await GetItemBinQuantityAsync( testSkuBin, _locationBostonBins.internalId, binNumber );
			updatedBinQuantity.Should().Be( newQuantity );
			var updatedNoBinQuantity = await this._itemsService.GetItemQuantityAsync( testSkuNoBin, _locationBostonBins.internalId, CancellationToken.None );
			updatedNoBinQuantity.Should().Be( newQuantityNoBin );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_ItemsNotInBins()
		{
			var inventory = new Dictionary< string, NetSuiteItemQuantity >();

			var random = new Random();
			
			for( int i = 1; i <= 18; i++ )
			{
				// item with bins
				if ( i == 6 )
					continue;

				var binQuantity = new NetSuiteItemQuantity
				{
					AvailableQuantity = random.Next( 1, 100 )
				};
				inventory.Add( "NS-testsku" + i.ToString(), binQuantity );
			}

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, _locationSkuVaultNoBins.name, inventory, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			foreach( var inventoryItem in inventory )
			{
				try
				{
					int currentQuantity = await this._itemsService.GetItemQuantityAsync( inventoryItem.Key, _locationSkuVaultNoBins.internalId, CancellationToken.None );
					currentQuantity.Should().Be( inventoryItem.Value.AvailableQuantity );
				}
				catch( NetSuiteItemNotFoundException )
				{ }
			}
		}

		[ Test ]
		public async Task GetItemInventoryAsync()
		{
			var soapService = new NetSuiteSoapService( this.Config );
			const string sku = "GUARD528-test1";
			var token = CancellationToken.None;
			var item = await soapService.GetItemBySkuAsync( sku, token );

			var itemInventory = await soapService.GetItemInventoryAsync( item, token, Mark.Blank() );

			itemInventory.Any( i => Math.Abs( i.quantityAvailable ) > 0 ).Should().BeTrue();
		}

		[ Test ]
		public async Task GetItemBySkuAsync_BinsInventory()
		{
			var soapService = new NetSuiteSoapService( this.Config );
			const string sku = "GUARD528-test1";
			var token = CancellationToken.None;
			
			var item = await soapService.GetItemBySkuAsync( sku, token );

			item.binNumberList.binNumber.Any( b => !string.IsNullOrEmpty( b.onHand ) ).Should().BeTrue();
		}

		[ Test ]
		public void UpdateParentSkuQuantityAsync()
		{
			const string sku = "TestParent3";

			var locationQuantity = new NetSuiteItemQuantity
			{
				AvailableQuantity = 10
			};
			var inventory = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ sku, locationQuantity }
			};
			Assert.DoesNotThrowAsync( async () =>
			{
				await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, _locationSkuVaultNoBins.name, inventory, NetSuiteInventoryBinsModeEnum.Both, CancellationToken.None, Mark.Blank() );
			} );
		}

		[ Test ]
		public void GetInventoryItemsCreatedAfterSpecifiedDate()
		{
			var createdDate = new DateTime( 2019, 12, 1 );
			var newItems = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createdDate, false, CancellationToken.None, Mark.Blank() ).Result;
			newItems.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetAllInventoryItems()
		{
			var newItems = this._itemsService.GetItemsCreatedUpdatedAfterAsync( DateTime.MinValue, true, CancellationToken.None, Mark.Blank() ).Result;
			newItems.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetInventoryItemsCreatedAndModifiedAfterSpecificDate()
		{
			var createOrModifiedDate = new DateTime( 2019, 12, 1 );
			var items = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createOrModifiedDate, true, CancellationToken.None, Mark.Blank() ).Result;
			items.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetEmptyInventoryItemsListCreatedAndModifiedAfterSpecificDate()
		{
			var createOrModifiedDate = new DateTime( 2100, 12, 1 );
			var items = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createOrModifiedDate, true, CancellationToken.None, Mark.Blank() ).Result;
			items.Count().Should().Be( 0 );
		}
	}
}