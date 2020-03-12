using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class GetPurchaseOrderCommand : NetSuiteCommand
	{
		public long OrderId { get; private set; }

		public GetPurchaseOrderCommand( NetSuiteConfig config, long orderId ) : base( config, "/rest/platform/v1/record/purchaseorder" )
		{
			Condition.Requires( orderId, "orderId" ).IsGreaterThan( 0 );

			this.OrderId = orderId;
			this.Url = string.Format( "{0}/{1}?expandSubResources=true", this.RelativeUrl, this.OrderId );
		}
	}
}