﻿using FluentAssertions;
using Netco.Logging;
using NetSuiteAccess.Services.Items;
using NetSuiteAccess.Services.Soap;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class ItemTests : BaseTest
	{
		private INetSuiteItemsService _itemsService;
		private const string Location = "SkuVault";
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

			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, ItemTests.Location, testSku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			int quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testSku, ItemTests.Location, CancellationToken.None );
			quantityAfterUpdate.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_BinFeatureEnabledOnlyOnItemLevel()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "NS-testsku555";
			
			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, ItemTests.Location, testsku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			int updatedQuantity = await this._itemsService.GetItemQuantityAsync( testsku, ItemTests.Location, CancellationToken.None );
			updatedQuantity.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_BinFeatureEnabledOnlyOnLocationLevel()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testSku = "NS-testsku1";
			const string locationUsesBins = "Boston";

			await _itemsService.UpdateItemQuantityBySkuAsync( AccountId, locationUsesBins, testSku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			var quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testSku, locationUsesBins, CancellationToken.None );
			quantityAfterUpdate.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_BothLocationAndItemUseBins()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testSku = "GUARD528-test1";
			const string locationUsesBins = "Boston";
			var initialQuantity = await this._itemsService.GetItemQuantityAsync( testSku, locationUsesBins, CancellationToken.None );

			await _itemsService.UpdateItemQuantityBySkuAsync( AccountId, locationUsesBins, testSku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			var quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testSku, locationUsesBins, CancellationToken.None );
			quantityAfterUpdate.Should().Be( initialQuantity );
		}

		[ Test ]
		public async Task UpdateItemQuantityBySkuAsync_NotPushingToBins_ModeSetToPushToNotInBinOnly_ItemAndLocationUsesBins()
		{
			var newQuantity = new Random().Next( 1, 100 );
			const string testsku = "NS-testsku555";
			const string locationName = "Boston";
			var initialQuantity = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );
			
			await this._itemsService.UpdateItemQuantityBySkuAsync( AccountId, locationName,  testsku, newQuantity, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			var quantityAfterUpdate = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );
			quantityAfterUpdate.Should().Be( initialQuantity );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_PushingToBins_ItemAndLocationUsesBins()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "GUARD528-test1";
			const string locationName = "Boston";
			const string locationId = "1";
			const string binNumber = "1004";
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
				{ testsku, itemQuantity }
			};
			var binQuantityBefore = await GetItemBinQuantityAsync( testsku, locationId, binNumber );
			var itemQuantityBefore = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var binQuantityAfter = await GetItemBinQuantityAsync( testsku, locationId, binNumber );;
			binQuantityAfter.Should().Be( newQuantity );
			var itemQuantityAfter = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );
			var quantityChange = binQuantityAfter - binQuantityBefore;
			itemQuantityAfter.Should().Be( itemQuantityBefore + quantityChange );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_PushingToBins_ZerosOutBinsNotPushed()
		{
			var nonZeroQuantity = new Random().Next( 1, 100 );
			const string testsku = "GUARD528-test1";
			const string locationName = "Boston";
			const string locationId = "1";
			const string binNumberToZeroOut = "1004";
			const string anotherBinNumber = "1001";
			var token = CancellationToken.None;

			var itemQuantity = new NetSuiteItemQuantity
			{
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( locationName, binNumberToZeroOut, nonZeroQuantity )
				}
			};
			var skuLocationQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testsku, itemQuantity }
			};
			//Push non-zero to binNumberToZeroOut
			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			//Push to anotherBinNumber
			itemQuantity.BinQuantities.First().BinNumber = anotherBinNumber;
			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var binQuantityAfter = await GetItemBinQuantityAsync( testsku, locationId, binNumberToZeroOut );
			binQuantityAfter.Should().Be( 0 );
			//Restore binNumberToZeroOut for other tests
			itemQuantity.BinQuantities.First().BinNumber = binNumberToZeroOut;
			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_PushingToBins_BinAssociatedWithItemButInAnotherLocation()
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
		public async Task UpdateSkusQuantitiesAsync_PushingToBins_ItemAndLocationUsesBins_BinIsNotAssociatedWithItem()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testsku = "GUARD528-test1";
			const string locationName = "Boston";
			const string locationId = "1";
			const string binNumber = "1999";
			var token = CancellationToken.None;
			var itemQuantity = new NetSuiteItemQuantity
			{
				AvailableQuantity = 0,
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( locationName, binNumber, newQuantity )
				}
			};
			var skuLocationQuantities = new Dictionary< string, NetSuiteItemQuantity >
			{
				{ testsku, itemQuantity }
			};
			var binQuantityBefore = await GetItemBinQuantityAsync( testsku, locationId, binNumber );
			var itemQuantityBefore = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuLocationQuantities, 
				NetSuiteInventoryBinsModeEnum.ItemsInBins, token, Mark.Blank() );

			var updatedBinQuantity = await GetItemBinQuantityAsync( testsku, locationId, binNumber );;
			updatedBinQuantity.Should().Be( binQuantityBefore );
			var itemQuantityAfter = await this._itemsService.GetItemQuantityAsync( testsku, locationName, CancellationToken.None );
			itemQuantityAfter.Should().Be( itemQuantityBefore );
		}

		private async Task< int > GetItemBinQuantityAsync( string sku, string locationId, string binNumber )
		{
			var inventoryItem = await new NetSuiteSoapService( this.Config ).GetItemBySkuAsync( sku, CancellationToken.None );
			return int.Parse( inventoryItem.binNumberList?.binNumber?.FirstOrDefault(
				b => b.location == locationId && b.binNumber.name.Equals( binNumber, StringComparison.InvariantCultureIgnoreCase ) )?.onHand ?? "0" );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync_PushItemsThatUsesBinsAndItemThatDoesnt()
		{
			int newQuantity = new Random().Next( 1, 100 );
			const string testSkuBin = "GUARD528-test1";
			const string locationName = "Boston";
			const string locationId = "1";
			const string binNumber = "1004";

			int newQuantityNoBin = new Random().Next( 1, 100 );
			const string testSkuNoBin = "NS-testsku99";

			var token = CancellationToken.None;
			var binQuantities = new NetSuiteItemQuantity
			{
				BinQuantities = new []
				{
					new NetSuiteBinQuantity( locationName, binNumber, newQuantity )
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

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, locationName, skuBinQuantities, 
				NetSuiteInventoryBinsModeEnum.Both, token, Mark.Blank() );

			var updatedBinQuantity = await GetItemBinQuantityAsync( testSkuBin, locationId, binNumber );
			updatedBinQuantity.Should().Be( newQuantity );
			var updatedNoBinQuantity = await this._itemsService.GetItemQuantityAsync( testSkuNoBin, locationName, CancellationToken.None );
			updatedNoBinQuantity.Should().Be( newQuantityNoBin );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync__NotPushingToBins_MultipleItems()
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

			await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, Location, inventory, NetSuiteInventoryBinsModeEnum.ItemsNotInBins, CancellationToken.None, Mark.Blank() );

			foreach( var inventoryItem in inventory )
			{
				try
				{
					int currentQuantity = await this._itemsService.GetItemQuantityAsync( inventoryItem.Key, Location, CancellationToken.None );
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
				await this._itemsService.UpdateSkusQuantitiesAsync( AccountId, Location, inventory, NetSuiteInventoryBinsModeEnum.Both, CancellationToken.None, Mark.Blank() );
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