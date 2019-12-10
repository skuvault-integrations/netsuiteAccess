using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;
using System;
using System.Globalization;

namespace NetSuiteAccess.Models.Commands
{
	public class GetModifiedPurchaseOrdersCommand : NetSuiteCommand
	{
		public DateTime StartDate { get; private set; }
		public DateTime EndDate { get; private set; }

		public GetModifiedPurchaseOrdersCommand( NetSuiteConfig config, DateTime startDate, DateTime endDate ) : base( config, "/rest/platform/v1/record/purchaseorder" )
		{
			Condition.Requires( startDate, "startDate" ).IsLessThan( endDate );

			this.StartDate = startDate;
			this.EndDate = endDate;

			this.Url = string.Format( "{0}?q=lastModifiedDate ON_OR_AFTER \"{1}\" AND lastModifiedDate ON_OR_BEFORE \"{2}\"", 
								this.RelativeUrl, 
								this.StartDate.ToString( "dd-MMM-yy HH:mm:ss", CultureInfo.InvariantCulture ),
								this.EndDate.ToString( "dd-MMM-yy HH:mm:ss", CultureInfo.InvariantCulture ) );
		}
	}
}
