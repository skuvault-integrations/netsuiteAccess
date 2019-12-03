using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class GetOrderCommand : NetSuiteCommand
	{
		public long OrderId { get; private set; }

		public GetOrderCommand( NetSuiteConfig config, long orderId ) : base( config, "/rest/platform/v1/record/salesorder" )
		{
			Condition.Requires( orderId, "orderId" ).IsGreaterThan( 0 );

			this.OrderId = orderId;
			this.Url = string.Format( "{0}/{1}?expandSubResources=true", this.RelativeUrl, this.OrderId );
		}
	}
}