using FluentAssertions;
using NetSuiteAccess.Models;
using NetSuiteSoapWS;
using NUnit.Framework;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class LocationMapperTests
	{
		[ Test ]
		public void LocationToSVLocation()
		{
			var netSuiteLocation = new Location()
			{
				internalId = "56",
				name = "SkuVault",
				useBins = true
			};

			var svLocation = netSuiteLocation.ToSVLocation();

			svLocation.Id.ToString().Should().Be( netSuiteLocation.internalId );
			svLocation.Name.Should().Be( netSuiteLocation.name );
			svLocation.UseBins.Should().Be( netSuiteLocation.useBins );
		}
	}
}