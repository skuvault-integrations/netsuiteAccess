using CuttingEdge.Conditions;

namespace NetSuiteAccess.Configuration
{
	public class NetSuiteConfig
	{
		public NetSuiteCredentials Credentials { get; private set; }

		public readonly string ApiBaseUrl;

		public readonly ThrottlingOptions ThrottlingOptions;
		public readonly NetworkOptions NetworkOptions;

		public int SearchRecordsPageSize { get; set; }
		public int SearchPurchaseOrdersPageSize { get; set; }

		public static int GetCustomersByIdsPageSize = 100;

		public NetSuiteConfig( NetSuiteCredentials credentials, ThrottlingOptions throttlingOptions, NetworkOptions networkOptions )
		{
			Condition.Requires( credentials, "credentials" ).IsNotNull();
			Condition.Requires( throttlingOptions, "throttlingOptions" ).IsNotNull();
			Condition.Requires( networkOptions, "networkOptions" ).IsNotNull();

			this.Credentials = credentials;
			this.ThrottlingOptions = throttlingOptions;
			this.NetworkOptions = networkOptions;
			this.ApiBaseUrl = string.Format( "https://{0}.suitetalk.api.netsuite.com", credentials.CustomerId );
			this.SearchRecordsPageSize = 100;
			this.SearchPurchaseOrdersPageSize = 50;
		}

		public NetSuiteConfig( NetSuiteCredentials credentials ) : this( credentials, ThrottlingOptions.NetSuiteDefaultThrottlingOptions, NetworkOptions.NetSuiteDefaultNetworkOptions )
		{}
	}

	public class ThrottlingOptions
	{
		public int MaxRequestsPerTimeInterval { get; private set; }
		public int TimeIntervalInSec { get; private set; }
		public int MaxRetryAttempts { get; private set; }

		public ThrottlingOptions( int maxRequests, int timeIntervalInSec, int maxRetryAttempts )
		{
			Condition.Requires( maxRequests, "maxRequests" ).IsGreaterOrEqual( 1 );
			Condition.Requires( timeIntervalInSec, "timeIntervalInSec" ).IsGreaterOrEqual( 1 );
			Condition.Requires( maxRetryAttempts, "maxRetryAttempts" ).IsGreaterOrEqual( 0 );

			this.MaxRequestsPerTimeInterval = maxRequests;
			this.TimeIntervalInSec = timeIntervalInSec;
			this.MaxRetryAttempts = maxRetryAttempts;
		}

		public static ThrottlingOptions NetSuiteDefaultThrottlingOptions
		{
			get
			{
				return new ThrottlingOptions( 4, 1, 10 );
			}
		}
	}

	public class NetworkOptions
	{
		public int RequestTimeoutMs { get; private set; }
		public int RetryAttempts { get; private set; }
		public int DelayBetweenFailedRequestsInSec { get; private set; }
		public int DelayFailRequestRate { get; private set; }

		public NetworkOptions( int requestTimeoutMs, int retryAttempts, int delayBetweenFailedRequestsInSec, int delayFaileRequestRate )
		{
			Condition.Requires( requestTimeoutMs, "requestTimeoutMs" ).IsGreaterThan( 0 );
			Condition.Requires( retryAttempts, "retryAttempts" ).IsGreaterOrEqual( 0 );
			Condition.Requires( delayBetweenFailedRequestsInSec, "delayBetweenFailedRequestsInSec" ).IsGreaterOrEqual( 0 );
			Condition.Requires( delayFaileRequestRate, "delayFaileRequestRate" ).IsGreaterOrEqual( 0 );

			this.RequestTimeoutMs = requestTimeoutMs;
			this.RetryAttempts = retryAttempts;
			this.DelayBetweenFailedRequestsInSec = delayBetweenFailedRequestsInSec;
			this.DelayFailRequestRate = delayFaileRequestRate;
		}

		public static NetworkOptions NetSuiteDefaultNetworkOptions
		{
			get
			{
				return new NetworkOptions( 10 * 60 * 1000, 10, 5, 20 );
			}
		}
	}
}