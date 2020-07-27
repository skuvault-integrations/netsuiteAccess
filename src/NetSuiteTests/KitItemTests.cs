using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Netco.Logging;
using NetSuiteAccess.Services.Soap;
using NetSuiteSoapWS;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class KitItemTests : BaseTest
	{
		private NetSuiteSoapService _soapService;

		[ SetUp ]
		public void Init()
		{
			this._soapService = new NetSuiteSoapService( this.Config );
		}

		[ Test ]
		public async Task GetKitItemBySkuAsync()
		{
			const string sku = "Test Kit";   //"1460";

			var kitItem = await _soapService.GetKitItemBySkuAsync( sku, CancellationToken.None, Mark.Blank() );

			kitItem.Should().NotBeNull();
		}

		[ Test ]
		public async Task GetKitsModifiedAfterAsync()
		{
			var modifiedDateUtc = DateTime.UtcNow.AddMonths( -2 );

			var items = await _soapService.GetItemsModifiedAfterAsync( modifiedDateUtc, CancellationToken.None, Mark.Blank() );

			items.OfType< KitItem >().Should().NotBeEmpty();
		}
	}
}
