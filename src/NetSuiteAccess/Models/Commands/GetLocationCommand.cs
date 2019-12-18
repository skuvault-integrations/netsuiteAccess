using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class GetLocationCommand : NetSuiteCommand
	{
		public long LocationId { get; private set; }

		public GetLocationCommand( NetSuiteConfig config, long locationId ) : base( config, "/rest/platform/v1/record/location" )
		{
			Condition.Requires( locationId, "locationId" ).IsGreaterThan( 0 );

			this.LocationId = locationId;
			this.Url = string.Format( "{0}/{1}", this.RelativeUrl, this.LocationId.ToString() );
		}
	}
}