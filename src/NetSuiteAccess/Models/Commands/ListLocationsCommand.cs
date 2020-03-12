using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class ListLocationsCommand : NetSuiteCommand
	{
		public ListLocationsCommand( NetSuiteConfig config ) : base( config, "/rest/platform/v1/record/location" )
		{
			this.Url = this.RelativeUrl;
		}
	}
}