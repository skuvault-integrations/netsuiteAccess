using System.Linq;
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
			const string subsidiaryId = "123";
			var netSuiteLocation = new Location()
			{
				internalId = "56",
				name = "SkuVault",
				useBins = true,
				subsidiaryList = new []
				{
					new RecordRef { internalId = subsidiaryId } 
				}
			};

			var svLocation = netSuiteLocation.ToSVLocation();

			svLocation.Id.ToString().Should().Be( netSuiteLocation.internalId );
			svLocation.Name.Should().Be( netSuiteLocation.name );
			svLocation.UseBins.Should().Be( netSuiteLocation.useBins );
			var svSubsidiaries = svLocation.Subsidiaries.ToList();
			svSubsidiaries.Count().Should().Be( netSuiteLocation.subsidiaryList.Length );
			svSubsidiaries.First().Should().Be( subsidiaryId );
		}
	}
}