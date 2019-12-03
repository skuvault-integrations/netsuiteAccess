using System;

namespace NetSuiteAccess.Exceptions
{
	public class NetSuiteNetworkException : NetSuiteException
	{
		public NetSuiteNetworkException( string message, Exception innerException ) : base( message, innerException) { }
		public NetSuiteNetworkException( string message ) : base( message ) { }
	}

	public class NetSuiteUnauthorizedException : NetSuiteException
	{
		public NetSuiteUnauthorizedException( string message ) : base( message) { }
	}

	public class NetSuiteRateLimitsExceeded : NetSuiteNetworkException
	{
		public NetSuiteRateLimitsExceeded( string message ) : base( message ) { }
	}
}