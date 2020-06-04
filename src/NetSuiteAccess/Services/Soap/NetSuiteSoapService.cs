using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Shared;
using NetSuiteAccess.Throttling;
using NetSuiteSoapWS;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Soap
{
	public class NetSuiteSoapService
	{
		private Throttler _throttler;
		private Func< string > _additionalLogInfo;
		/// <summary>
		///	Extra logging information
		/// </summary>
		public Func< string > AdditionalLogInfo
		{
			get { return this._additionalLogInfo ?? ( () => string.Empty ); }
			set => _additionalLogInfo = value;
		}

		private NetSuiteConfig _config; 
		private NetSuitePortTypeClient _service;

		/// <summary>
		///	Token-based authentication.
		///	TBA option should be enabled in the application integration settings.
		/// </summary>
		private TokenPassport _passport
		{
			get
			{
				return this.CreateTokenPassport();
			}
		}

		public NetSuiteSoapService( NetSuiteConfig config )
		{
			Condition.Requires( config, "config" ).IsNotNull();

			this._config = config;
			this._service = new NetSuitePortTypeClient( NetSuitePortTypeClient.EndpointConfiguration.NetSuitePort );
			this.ConfigureClient( this._service );

			this._throttler = new Throttler( config.ThrottlingOptions.MaxRequestsPerTimeInterval, config.ThrottlingOptions.TimeIntervalInSec, config.ThrottlingOptions.MaxRetryAttempts );
		}

		private void ConfigureClient( NetSuitePortTypeClient client )
		{
			client.Endpoint.Binding.SendTimeout = new TimeSpan( 0, 0, 0, 0, this._config.NetworkOptions.RequestTimeoutMs );

			string subdomain = this._config.Credentials.CustomerId.ToLowerInvariant().Replace("_", "-");
			client.Endpoint.Address = this.GetDataCenterEndpoint( client, $"https://{ subdomain }.suitetalk.api.netsuite.com" );
		}

		private EndpointAddress GetDataCenterEndpoint( NetSuitePortTypeClient client, string dataCenter)
		{
			var endpoint = client.Endpoint.Address;
			var relativeWsPath = endpoint.Uri.LocalPath;

			if ( !dataCenter.EndsWith( "/" ) )
			{
				return new EndpointAddress( dataCenter + relativeWsPath );
			}
			else
			{
				return new EndpointAddress( string.Concat( dataCenter.Substring( 0, dataCenter.Length - 1), relativeWsPath ) );
			}
		}

		/// <summary>
		///	Find inventory item in NetSuite by sku.
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="sku"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< InventoryItem > GetItemBySkuAsync( string sku, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var searchRecord = new ItemSearch()
			{
				basic = new ItemSearchBasic()
				{
					itemId = new SearchStringField()
					{
						@operator = SearchStringFieldOperator.@is,
						operatorSpecified = true,
						searchValue = sku
					}
				}
			};
		
			var response = await this.SearchRecords( searchRecord, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< InventoryItem >().FirstOrDefault();
		}

		/// <summary>
		///	Find inventory items created after specified date.
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="createdDateUtc"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task< IEnumerable< Record > > GetItemsCreatedAfterAsync( DateTime createdDateUtc, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var itemsSearch = new ItemSearchBasic()
			{
				 created = new SearchDateField()
				 {
					@operator = SearchDateFieldOperator.onOrAfter,
					operatorSpecified = true,
					searchValue = createdDateUtc == DateTime.MinValue ? new DateTime( 1970, 1, 1) : createdDateUtc,
					searchValueSpecified = true
				 }, 
				 isInactive = new SearchBooleanField()
				 {
					 searchValue = false,
					 searchValueSpecified = true
				 }
			};

			return this.SearchRecords( itemsSearch, mark, cancellationToken );
		}

		/// <summary>
		///	Find inventory items modified after specified date.
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="modifiedDateUtc"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task< IEnumerable< Record > > GetItemsModifiedAfterAsync( DateTime modifiedDateUtc, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var itemsSearch = new ItemSearchBasic()
			{
				 lastModifiedDate = new SearchDateField()
				 {
					@operator = SearchDateFieldOperator.onOrAfter,
					operatorSpecified = true,
					searchValue = modifiedDateUtc == DateTime.MinValue ? new DateTime( 1970, 1, 1 ) : modifiedDateUtc,
					searchValueSpecified = true
				 },
				 isInactive = new SearchBooleanField()
				 {
					 searchValue = false,
					 searchValueSpecified = true
				 }
			};

			return this.SearchRecords( itemsSearch, mark, cancellationToken );
		}

		/// <summary>
		///	Get item inventory. It supports multi-location inventory feature.
		///	Requires Lists -> Items role permission.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< ItemAvailability[] > GetItemInventoryAsync( InventoryItem item, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var filter = new ItemAvailabilityFilter()
			{
				item = new RecordRef[] { new RecordRef() { internalId = item.internalId } }
			};

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.getItemAvailabilityAsync( null, this._passport, null, null, null, filter );
			}, item.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( response.getItemAvailabilityResult.status.isSuccess )
			{
				return response.getItemAvailabilityResult.itemAvailabilityList;
			}
			
			throw new NetSuiteException( response.getItemAvailabilityResult.status.statusDetail[0].message );
		}

		/// <summary>
		///	Adjust items inventory. 
		///	Requires Transactions -> Adjust Inventory role permission. Level - Edit.
		/// </summary>
		/// <param name="accountId"></param>
		/// <param name="inventory"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task AdjustInventoryAsync( int accountId, InventoryAdjustmentInventory[] inventory, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var adjustment = new InventoryAdjustment()
			{
				 account = new RecordRef()
				 {
					 internalId = accountId.ToString()
				 },
				 inventoryList = new InventoryAdjustmentInventoryList()
				 {
					 inventory = inventory
				 }, 
			};

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.addAsync( null, this._passport, null, null, null, adjustment );
			}, adjustment.ToJson(), cancellationToken );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		/// <summary>
		///	Lists all financial accounts.
		///	Requires Lists -> Accounts role permission. Level - View.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteAccount > > ListAccountsAsync( CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var accountsSearch = new AccountSearch();

			var response = await this.SearchRecords( accountsSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Account >().Select( r => new NetSuiteAccount() { Id = int.Parse( r.internalId ), Name = r.acctName, Number = r.acctNumber } );
		}

		/// <summary>
		///	Lists all locations
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteLocation > > ListLocationsAsync( CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var locationsSearch = new LocationSearch();
			var response = await this.SearchRecords( locationsSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Location >().Select( l => l.ToSVLocation() );
		}

		/// <summary>
		///	Get location info by name
		/// </summary>
		/// <param name="locationName"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< NetSuiteLocation > GetLocationByNameAsync( string locationName, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var locationsSearch = new LocationSearch()
			{
				basic = new LocationSearchBasic()
				{
					name = new SearchStringField()
					{
						searchValue = locationName,
						operatorSpecified = true,
						@operator = SearchStringFieldOperator.contains
					}
				}
			};

			var response = await this.SearchRecords( locationsSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Location >().Where( l => l.name.Equals( locationName ) )
									.Select( l => l.ToSVLocation() ).FirstOrDefault();
		}

		/// <summary>
		///	Find vendor by name.
		///	Requires Lists -> Vendors role permission. Level - View.
		/// </summary>
		/// <param name="vendorName">Vendor name</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< Vendor > GetVendorByNameAsync( string vendorName, CancellationToken cancellationToken )
		{
			if ( string.IsNullOrWhiteSpace( vendorName ) )
				return null;

			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var vendorSearch = new VendorSearch()
			{
				basic = new VendorSearchBasic()
				{
					entityId = new SearchStringField()
					{
						@operator = SearchStringFieldOperator.@is,
						operatorSpecified = true,
						searchValue = vendorName
					}
				}
			};

			var response = await this.SearchRecords( vendorSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Vendor >().FirstOrDefault();
		}

		/// <summary>
		///	Find customer by internal id
		/// </summary>
		/// <param name="internalId"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< IEnumerable< Customer > > GetCustomersByIdsAsync( IEnumerable< string > customersIds, CancellationToken cancellationToken )
		{
			if ( customersIds == null || !customersIds.Any() )
				return null;

			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var customerSearch = new CustomerSearch()
			{
				basic = new CustomerSearchBasic()
				{
					internalId = new SearchMultiSelectField()
					{
						 @operator = SearchMultiSelectFieldOperator.anyOf,
						 operatorSpecified = true,
						 searchValue = customersIds.Select( id => new RecordRef() { internalId = id } ).ToArray()
					}
				}
			};

			var response = await this.SearchRecords( customerSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Customer >();
		}

		/// <summary>
		///	Find customer by email
		/// </summary>
		/// <param name="email"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< Customer > GetCustomerByEmailAsync( string email, CancellationToken cancellationToken )
		{
			if ( string.IsNullOrWhiteSpace( email ) )
				return null;

			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var customerSearch = new CustomerSearch()
			{
				basic = new CustomerSearchBasic()
				{
					email = new SearchStringField()
					{
						@operator = SearchStringFieldOperator.@is,
						operatorSpecified = true,
						searchValue = email
					}
				}
			};

			var response = await this.SearchRecords( customerSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< Customer >().FirstOrDefault();
		}

		/// <summary>
		///	Create purchase order.
		///	Requires Transactions -> Purchase Order role permission. Level - Create or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="locationId"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task CreatePurchaseOrderAsync( NetSuitePurchaseOrder order, long locationId, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var vendor = await this.GetVendorByNameAsync( order.SupplierName, cancellationToken ).ConfigureAwait( false );

			if ( vendor == null )
			{
				NetSuiteLogger.LogTrace( string.Format( "Can't create purchase order in NetSuite! Vendor with name {0} doesn't exist in NetSuite!", order.SupplierName ) );
				return;
			}

			var purchaseOrderRecord = new NetSuiteSoapWS.PurchaseOrder()
			{
				tranId = order.DocNumber,
				createdDate = order.CreatedDateUtc,
				location = new RecordRef()
				{
					internalId = locationId.ToString()
				}, 
				entity = new RecordRef()
				{
					internalId = vendor.internalId
				}, 
				memo = order.PrivateNote
			};

			var purchaseOrderRecordItems = new List< PurchaseOrderItem >();
			foreach( var orderItem in order.Items )
			{
				var item = await this.GetItemBySkuAsync( orderItem.Sku, cancellationToken ).ConfigureAwait( false );

				if ( item != null)
				{
					purchaseOrderRecordItems.Add( new PurchaseOrderItem()
					{
						item = new RecordRef() { internalId = item.internalId }, 
						quantity = orderItem.Quantity,
						quantitySpecified = true,
						rate = orderItem.UnitPrice.ToString(),
						quantityBilled = orderItem.Quantity,
						quantityBilledSpecified = order.PaymentStatus == NetSuitePaymentStatus.Paid, 
					} );
				}
			}

			if ( purchaseOrderRecordItems.Count == 0 )
			{
				NetSuiteLogger.LogTrace( "Can't create purchase order in NetSuite! PO items don't exist in NetSuite!" );
				return;
			}

			purchaseOrderRecord.itemList = new PurchaseOrderItemList() { item = purchaseOrderRecordItems.ToArray() };

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.addAsync( null, this._passport, null, null, null, purchaseOrderRecord );
			}, order.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		/// <summary>
		///	Update purchase order
		///	Requires Transactions -> Purchase Order role permission. Level - Edit or Full.
		/// </summary>
		/// <param name="order"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task UpdatePurchaseOrderAsync( NetSuitePurchaseOrder order, CancellationToken cancellationToken )
		{
			if ( !(new NetSuitePurchaseOrderStatus[] { 
					NetSuitePurchaseOrderStatus.PendingReceipt, 
					NetSuitePurchaseOrderStatus.PartiallyReceived,
					NetSuitePurchaseOrderStatus.PendingBillingPartiallyReceived }.Contains( order.Status ) ) )
			{
				return;
			}

			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			// set order's items as received if necessary, order cannot be changed further
			if ( order.Items.Any( i => i.ReceivedQuantity > 0 )
				|| order.Status != NetSuitePurchaseOrderStatus.PendingReceipt )
			{
				// purchase orders created from sales orders cannot have item receipts
				if ( order.CreatedFrom == null )
				{
					await this.ReceiveOrder( order, cancellationToken ).ConfigureAwait( false );
					return;
				}
			}

			var vendor = await this.GetVendorByNameAsync( order.SupplierName, cancellationToken ).ConfigureAwait( false );

			if ( vendor == null )
			{
				NetSuiteLogger.LogTrace( string.Format( "Can't create purchase order in NetSuite! Vendor with name {0} doesn't exist in NetSuite!", order.SupplierName ) );
				return;
			}

			var purchaseOrderRecord = new NetSuiteSoapWS.PurchaseOrder()
			{
				internalId = order.Id,
				tranId = order.DocNumber,
				entity = new RecordRef()
				{
					internalId = vendor.internalId
				}, 
				memo = order.PrivateNote
			};

			var purchaseOrderRecordItems = new List< PurchaseOrderItem >();
			if ( order.Status == NetSuitePurchaseOrderStatus.PendingReceipt )
			{
				foreach( var orderItem in order.Items )
				{
					var item = await this.GetItemBySkuAsync( orderItem.Sku, cancellationToken ).ConfigureAwait( false );

					if ( item != null )
					{
						purchaseOrderRecordItems.Add( new PurchaseOrderItem()
						{
							item = new RecordRef() { internalId = item.internalId }, 
							quantity = orderItem.Quantity,
							quantitySpecified = true,
							rate = orderItem.UnitPrice.ToString( CultureInfo.InvariantCulture )
						} );
					}
				}

				purchaseOrderRecord.itemList = new PurchaseOrderItemList()
				{
					item = purchaseOrderRecordItems.ToArray()
				};
			}

			if ( purchaseOrderRecordItems.Count == 0 )
			{
				NetSuiteLogger.LogTrace( "Can't update purchase order in NetSuite! PO items don't exist in NetSuite!" );
				return;
			}

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.updateAsync( null, this._passport, null, null, null, purchaseOrderRecord );
			}, purchaseOrderRecord.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		/// <summary>
		///	Receive order
		/// </summary>
		/// <param name="purchaseOrder"></param>
		/// <param name="itemsIds"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task ReceiveOrder( NetSuitePurchaseOrder purchaseOrder, CancellationToken cancellationToken )
		{
			var existingItemsReceipt = await this.SearchItemsReceipt( purchaseOrder, cancellationToken ).ConfigureAwait( false );

			if ( existingItemsReceipt == null )
			{
				await this.CreateItemsReceipt( purchaseOrder, cancellationToken ).ConfigureAwait( false );
				return;
			}

			await this.UpdateItemsReceipt( existingItemsReceipt, purchaseOrder, cancellationToken ).ConfigureAwait( false );
		}

		/// <summary>
		///	Create new items receipt document
		/// </summary>
		/// <param name="purchaseOrder"></param>
		/// <param name="itemsIds"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task CreateItemsReceipt( NetSuitePurchaseOrder purchaseOrder, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			ItemReceipt itemsReceipt = null;
			var initResponse = await this._service.initializeAsync( null, this._passport, null, null, null, 
				new InitializeRecord() { 
					reference = new InitializeRef() { 
						internalId = purchaseOrder.Id, 
						type = InitializeRefType.purchaseOrder, 
						typeSpecified = true
					}, 
					type = InitializeType.itemReceipt } );

			if ( !initResponse.readResponse.status.isSuccess )
				throw new NetSuiteException( initResponse.readResponse.status.statusDetail[0].message );
			
			itemsReceipt = initResponse.readResponse.record as ItemReceipt;
			if ( itemsReceipt != null )
			{
				// otherwise we'll receive error from NetSuite
				itemsReceipt.exchangeRateSpecified = false;

				foreach( var itemReceiptItem in itemsReceipt.itemList.item )
				{
					var poItem = purchaseOrder.Items.FirstOrDefault( i => i.Sku.ToLower().Equals( itemReceiptItem.item.name.ToLower() ) );

					if ( poItem != null )
					{
						itemReceiptItem.quantity = poItem.ReceivedQuantity;
					}
				}

				var response = await this.ThrottleRequestAsync( mark, ( token ) =>
				{
					return this._service.addAsync( null, this._passport, null, null, null, itemsReceipt );
				}, itemsReceipt.ToJson(), cancellationToken ).ConfigureAwait( false );

				if ( !response.writeResponse.status.isSuccess )
				{
					throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
				}
			}
		}

		/// <summary>
		///	Update items receipt document
		/// </summary>
		/// <param name="purchaseOrder"></param>
		/// <param name="itemsIds"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task UpdateItemsReceipt( ItemReceipt itemsReceipt, NetSuitePurchaseOrder purchaseOrder, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			foreach( var itemReceiptItem in itemsReceipt.itemList.item )
			{
				var poItem = purchaseOrder.Items.FirstOrDefault( i => i.Sku.ToLower().Equals( itemReceiptItem.item.name.ToLower() ) );

				if ( poItem != null )
				{
					itemReceiptItem.quantity = poItem.ReceivedQuantity; 
				}
			}

			// otherwise we'll receive error from NetSuite
			itemsReceipt.exchangeRateSpecified = false;

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.updateAsync( null, this._passport, null, null, null, itemsReceipt );
			}, itemsReceipt.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		/// <summary>
		///	Find existing item receipt document related to purchase order
		/// </summary>
		/// <param name="purchaseOrder"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task< ItemReceipt > SearchItemsReceipt( NetSuitePurchaseOrder purchaseOrder, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();
			var itemsReceiptSearch = new TransactionSearchBasic()
			{
				createdFrom = new SearchMultiSelectField()
				{
					@operator = SearchMultiSelectFieldOperator.anyOf,
					operatorSpecified = true,
					searchValue = new RecordRef[] { new RecordRef() { internalId = purchaseOrder.Id } }
				}
			};

			var response = await this.SearchRecords( itemsReceiptSearch, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< ItemReceipt >().FirstOrDefault();
		}

		/// <summary>
		///	Lists all purchase orders
		/// </summary>
		/// <param name="order"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuitePurchaseOrder > > GetAllPurchaseOrdersAsync( CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var purchaseOrdersSearchRequest = new TransactionSearchBasic()
			{
				   recordType = new SearchStringField()
				   {
					    @operator = SearchStringFieldOperator.@is,
					    searchValue = "purchaseOrder",
					    operatorSpecified = true
				   }
			};

			var response = await this.SearchRecords( purchaseOrdersSearchRequest, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< NetSuiteSoapWS.PurchaseOrder >().Select( p => p.ToSVPurchaseOrder() );
		}

		public async Task< IEnumerable< NetSuitePurchaseOrder > > GetModifiedPurchaseOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var purchaseOrdersSearchRequest = new TransactionSearchBasic()
			{
				lastModifiedDate = new SearchDateField()
				{
						@operator = SearchDateFieldOperator.within,
						operatorSpecified = true,
						searchValue = startDateUtc,
						searchValueSpecified = true,
						searchValue2 = endDateUtc,
						searchValue2Specified = true
				},
				recordType = new SearchStringField()
				{
					@operator = SearchStringFieldOperator.@is,
					searchValue = "purchaseOrder",
					operatorSpecified = true
				},
			};

			var response = await this.SearchRecords( purchaseOrdersSearchRequest, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< NetSuiteSoapWS.PurchaseOrder >().Select( p => p.ToSVPurchaseOrder() );
		}

		/// <summary>
		///	Get all modified sales orders
		/// </summary>
		/// <returns></returns>
		public async Task< IEnumerable< NetSuiteSalesOrder > > GetModifiedSalesOrdersAsync( DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var searchModifiedSalesOrdersRequest = new TransactionSearchBasic()
			{
				lastModifiedDate = new SearchDateField()
				{
					 @operator = SearchDateFieldOperator.within,
					 operatorSpecified = true,
					 searchValue = startDateUtc.ToUniversalTime(),
					 searchValueSpecified = true,
					 searchValue2 = endDateUtc.ToUniversalTime(),
					 searchValue2Specified = true
				},
				recordType = new SearchStringField()
				{
					@operator = SearchStringFieldOperator.@is,
					searchValue = "salesOrder",
					operatorSpecified = true
				}
			};

			var response = await this.SearchRecords( searchModifiedSalesOrdersRequest, mark, cancellationToken ).ConfigureAwait( false );
			return response.OfType< NetSuiteSoapWS.SalesOrder >().Select( p => p.ToSVSalesOrder() );
		}

		private async Task< IEnumerable< Record > > SearchRecords( SearchRecord searchRecord, Mark mark, CancellationToken cancellationToken )
		{
			var result = new List< Record >();
			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				var searchPreferences = new SearchPreferences()
				{
					bodyFieldsOnly = false
				};

				return this._service.searchAsync( null, this._passport, null, null, searchPreferences, searchRecord );
			}, searchRecord.ToJson(), cancellationToken ).ConfigureAwait( false );

			var searchResult = response.searchResult;
			if ( searchResult.status.isSuccess )
			{
				result.AddRange( searchResult.recordList );

				if ( searchResult.totalPages > 1 )
				{
					result.AddRange( await this.CollectDataFromExtraPages< Record >( searchResult.searchId, searchResult.totalPages ).ConfigureAwait( false ) );
				}

				return result;
			}
			
			throw new NetSuiteException( response.searchResult.status.statusDetail[0].message );
		}

		private async Task< IEnumerable< T > > CollectDataFromExtraPages< T >( string searchId, int totalPages )
		{
			var result = new List< T >();
			int pageIndex = 2;
			while ( pageIndex <= totalPages )
			{
				var searchMoreResponse = await this._service.searchMoreWithIdAsync( null, this._passport, null, null, null, searchId, pageIndex ).ConfigureAwait( false );
				++pageIndex;

				if ( !searchMoreResponse.searchResult.status.isSuccess )
				{
					break;
				}

				result.AddRange( searchMoreResponse.searchResult.recordList.OfType< T >() );
			}

			return result;
		}

		/// <summary>
		///	Creates sales order
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task CreateSalesOrderAsync( NetSuiteSalesOrder order, long locationId, long customerId, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var record = new NetSuiteSoapWS.SalesOrder()
			{
				tranId = order.DocNumber,
				location = new RecordRef()
				{
					internalId = locationId.ToString()
				}, 
				entity = new RecordRef()
				{
					internalId = customerId.ToString()
				}, 
			};

			var recordItems = new List< NetSuiteSoapWS.SalesOrderItem >();
			foreach( var orderItem in order.Items )
			{
				var item = await this.GetItemBySkuAsync( orderItem.Sku, cancellationToken ).ConfigureAwait( false );

				if ( item != null)
				{
					recordItems.Add( new SalesOrderItem()
					{
						item = new RecordRef() { internalId = item.internalId }, 
						quantity = orderItem.Quantity,
						quantitySpecified = true,
						isClosed = order.Status == NetSuiteSalesOrderStatus.Closed ? true : false,
						isClosedSpecified = true, 
						amount = (double)orderItem.UnitPrice * orderItem.Quantity,
						amountSpecified = true
					} );
				}
			}

			if ( recordItems.Count == 0 )
			{
				NetSuiteLogger.LogTrace( "Can't create sales order in NetSuite! SO items don't exist in NetSuite!" );
				return;
			}

			record.itemList = new SalesOrderItemList() {  item = recordItems.ToArray() };

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.addAsync( null, this._passport, null, null, null, record );
			}, order.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		/// <summary>
		///	Updates sales order
		/// </summary>
		/// <param name="order">Sales order</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public async System.Threading.Tasks.Task UpdateSalesOrderAsync( NetSuiteSalesOrder order, long locationId, long customerId, CancellationToken cancellationToken )
		{
			var mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var record = new NetSuiteSoapWS.SalesOrder()
			{
				internalId = order.Id,
				entity = new RecordRef()
				{
					internalId = customerId.ToString()
				},
				location = new RecordRef()
				{
					internalId = locationId.ToString()
				}
			};

			var recordItems = new List< NetSuiteSoapWS.SalesOrderItem >();
			foreach( var orderItem in order.Items )
			{
				var item = await this.GetItemBySkuAsync( orderItem.Sku, cancellationToken ).ConfigureAwait( false );

				if ( item != null)
				{
					recordItems.Add( new SalesOrderItem()
					{
						item = new RecordRef() { internalId = item.internalId }, 
						quantity = orderItem.Quantity,
						quantitySpecified = true,
						isClosed = order.Status == NetSuiteSalesOrderStatus.Closed ? true : false,
						isClosedSpecified = true,
						amount = (double)orderItem.UnitPrice * orderItem.Quantity,
						amountSpecified = true
					} );
				}
			}

			if ( recordItems.Count == 0 )
			{
				NetSuiteLogger.LogTrace( "Can't update sales order in NetSuite! SO items don't exist in NetSuite!" );
				return;
			}

			record.itemList = new SalesOrderItemList() {  item = recordItems.ToArray() };

			var response = await this.ThrottleRequestAsync( mark, ( token ) =>
			{
				return this._service.updateAsync( null, this._passport, null, null, null, record );
			}, record.ToJson(), cancellationToken ).ConfigureAwait( false );

			if ( !response.writeResponse.status.isSuccess )
			{
				throw new NetSuiteException( response.writeResponse.status.statusDetail[0].message );
			}
		}

		private Task< T > ThrottleRequestAsync< T >( Mark mark, Func< CancellationToken, Task< T > > processor, string payload, CancellationToken token )
		{
			return this._throttler.ExecuteAsync( () =>
			{
				return new ActionPolicy( this._config.NetworkOptions.RetryAttempts, this._config.NetworkOptions.DelayBetweenFailedRequestsInSec, this._config.NetworkOptions.DelayFailRequestRate )
					.ExecuteAsync( async () =>
					{
						Misc.InitSecurityProtocol();

						using( var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource( token ) )
						{
							NetSuiteLogger.LogStarted( this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), payload: payload ) );
							linkedTokenSource.CancelAfter( this._config.NetworkOptions.RequestTimeoutMs );

							var result = await processor( linkedTokenSource.Token ).ConfigureAwait( false );

							NetSuiteLogger.LogEnd( this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), result: result.ToJson() ) );

							return result;
						}
					}, 
					( exception, timeSpan, retryCount ) =>
					{
						string retryDetails = this.CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo(), errors: exception.Message );
						NetSuiteLogger.LogTraceRetryStarted( timeSpan.Seconds, retryCount, retryDetails );
					},
					() => CreateMethodCallInfo( mark: mark, additionalInfo: this.AdditionalLogInfo() ),
					NetSuiteLogger.LogTraceException );
			} );
		}

		private string CreateMethodCallInfo( Mark mark = null, string errors = "", string result = "", string additionalInfo = "", string payload = "", [ CallerMemberName ] string memberName = "" )
		{
			var str = string.Format(
				"{{MethodName: {0}, Mark: '{1}', ServiceEndPoint: '{2}', {3} {4}{5}{6}}}",
				memberName,
				mark ?? Mark.Blank(),
				this._service.Endpoint.Address.Uri,
				string.IsNullOrWhiteSpace( errors ) ? string.Empty : ", Errors:" + errors,
				string.IsNullOrWhiteSpace( result ) ? string.Empty : ", Result:" + result,
				string.IsNullOrWhiteSpace( additionalInfo ) ? string.Empty : ", " + additionalInfo,
				string.IsNullOrWhiteSpace( payload ) ? string.Empty : ", Payload:" + payload
			);
			return str;
		}

		private TokenPassport CreateTokenPassport()
		{
			var tokenPassport = new TokenPassport()
			{
				account = this._config.Credentials.CustomerId.ToUpper(),
				consumerKey = this._config.Credentials.ConsumerKey,
				token = this._config.Credentials.TokenId,
				nonce = GetRandomSessionNonce(),
				timestamp = GetUtcEpochTime()
			};
			return this.SignPassport( tokenPassport );
		}

		private string GetRandomSessionNonce()
		{
			return Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 11 ).ToUpper();
		}

		private long GetUtcEpochTime()
		{
			return (int)( DateTime.UtcNow - new DateTime( 1970, 1, 1 ) ).TotalSeconds;
		}

		private TokenPassport SignPassport( TokenPassport passport )
		{
			string baseString = passport.account + "&" + passport.consumerKey + "&" + passport.token + "&" + passport.nonce + "&" + passport.timestamp;
			string key = this._config.Credentials.ConsumerSecret + "&" + this._config.Credentials.TokenSecret;
			string signature = string.Empty;
			
			var encoding = new ASCIIEncoding();
			byte[] keyBytes = encoding.GetBytes( key );
			byte[] baseStringBytes = encoding.GetBytes( baseString );
			using ( var hmacSha1 = new HMACSHA1( keyBytes ) )
			{
				byte[] hashBaseString = hmacSha1.ComputeHash( baseStringBytes );
				signature = Convert.ToBase64String( hashBaseString );
			}
			
			passport.signature = new TokenPassportSignature()
			{
				algorithm = "HMAC-SHA1",
				Value = signature
			};
			
			return passport;
		}
	}
}