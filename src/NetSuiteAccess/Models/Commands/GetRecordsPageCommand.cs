using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class GetRecordsPageCommand : NetSuiteCommand
	{
		public int Limit { get; set; }
		public int Offset { get; set; }

		public GetRecordsPageCommand( NetSuiteConfig config, NetSuiteCommand command, int limit, int offset ) : base( config, string.Empty )
		{
			Condition.Requires( limit, "limit" ).IsGreaterOrEqual( 0 );
			Condition.Requires( offset, "offset" ).IsGreaterOrEqual( 0 );

			this.Limit = limit;
			this.Offset = offset;

			this.Url = string.Format( "{0}{1}limit={2}&offset={3}", command.Url, command.Url.Contains( "?" ) ? "&" : "?", this.Limit, this.Offset );
		}
	}
}