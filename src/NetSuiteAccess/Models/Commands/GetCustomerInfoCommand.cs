using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public class GetCustomerInfoCommand : NetSuiteCommand
	{
		public long CustomerId { get; private set; }

		public GetCustomerInfoCommand( NetSuiteConfig config, long customerId ) : base( config, "/rest/platform/v1/record/customer" )
		{
			Condition.Requires( customerId, "customerId" ).IsGreaterThan( 0 );

			this.CustomerId = customerId;
			this.Url = string.Format( "{0}/{1}", this.RelativeUrl, this.CustomerId );
		}
	}
}