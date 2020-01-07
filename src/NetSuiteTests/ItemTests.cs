﻿using FluentAssertions;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Services.Items;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class ItemTests : BaseTest
	{
		private INetSuiteItemsService _itemsService;
		private const string warehouse = "SkuVault";
		private const int accountId = 54;

		[ SetUp ]
		public void Init()
		{
			this._itemsService = new NetSuiteItemsService( base.Config );
		}

		[ Test ]
		public async Task UpdateSkuQuantityAsync()
		{
			int newQuantity = new Random().Next( 1, 100 );
			string testSku = "NS-testsku1";

			await this._itemsService.UpdateItemQuantityBySkuAsync( accountId, warehouse, testSku, newQuantity, CancellationToken.None );

			int currentQuantity = await this._itemsService.GetSkuQuantity( testSku, warehouse, CancellationToken.None );
			currentQuantity.Should().Be( newQuantity );
		}

		[ Test ]
		public async Task UpdateSkuQuantityWithBinFeatureEnabled()
		{
			int newQuantity = 100;
			string testsku = "NS-testsku6";

			int currentQuantity = await this._itemsService.GetSkuQuantity( testsku, warehouse, CancellationToken.None );
			await this._itemsService.UpdateItemQuantityBySkuAsync( accountId, warehouse, testsku, newQuantity, CancellationToken.None );

			int updatedQuantity = await this._itemsService.GetSkuQuantity( testsku, warehouse, CancellationToken.None );
			updatedQuantity.Should().Be( currentQuantity );
		}

		[ Test ]
		public async Task UpdateSkusQuantitiesAsync()
		{
			var inventory = new Dictionary< string, int >();
			var random = new Random();

			for( int i = 1; i <= 18; i++ )
			{
				// item with bins
				if ( i == 6 )
					continue;

				inventory.Add( "NS-testsku" + i.ToString(), random.Next( 1, 100 ) );
			}

			await this._itemsService.UpdateSkusQuantitiesAsync( accountId, warehouse, inventory, CancellationToken.None );

			foreach( var inventoryItem in inventory )
			{
				try
				{
					int currentQuantity = await this._itemsService.GetSkuQuantity( inventoryItem.Key, warehouse, CancellationToken.None );
					currentQuantity.Should().Be( inventoryItem.Value );
				}
				catch( NetSuiteItemNotFoundException )
				{ }
			}
		}

		[ Test ]
		public void UpdateParentSkuQuantityAsync()
		{
			var inventory = new Dictionary< string, int >
			{
				{ "TestParent3", 10 }
			};

			Assert.DoesNotThrowAsync( async () =>
			{
				await this._itemsService.UpdateSkusQuantitiesAsync( accountId, warehouse, inventory, CancellationToken.None );
			} );
		}

		[ Test ]
		public void GetInventoryItemsCreatedAfterSpecifiedDate()
		{
			var createdDate = new DateTime( 2019, 12, 1 );
			var newItems = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createdDate, false, CancellationToken.None ).Result;
			newItems.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetInventoryItemsCreatedAndModifiedAfterSpecificDate()
		{
			var createOrModifiedDate = new DateTime( 2019, 12, 1 );
			var items = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createOrModifiedDate, true, CancellationToken.None ).Result;
			items.Count().Should().BeGreaterThan( 0 );
		}

		[ Test ]
		public void GetEmptyInventoryItemsListCreatedAndModifiedAfterSpecificDate()
		{
			var createOrModifiedDate = new DateTime( 2100, 12, 1 );
			var items = this._itemsService.GetItemsCreatedUpdatedAfterAsync( createOrModifiedDate, true, CancellationToken.None ).Result;
			items.Count().Should().Be( 0 );
		}
	}
}