﻿using System;

namespace NetSuiteAccess.Exceptions
{
	public class NetSuiteException : Exception
	{
		public NetSuiteException( string message, Exception innerException ) : base( message, innerException ) { }
		public NetSuiteException( string message ) : this ( message, null) { }
	}
}