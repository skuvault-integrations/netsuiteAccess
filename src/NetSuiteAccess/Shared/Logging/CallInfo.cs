using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace NetSuiteAccess.Shared.Logging
{
	public class CallInfo
	{
		public string Mark { get; set; }
		public string LibMethodName { get; set; }
		public string Endpoint { get; set; }
		public string RequestParameters { get; set; }
		public string Method { get; set; }
		public object Body { get; set; }
		public string AdditionalInfo { get; set; }
		public object Response { get; set; }
		public string Errors { get; set; }

		public static string CreateInfo( string url = null, Mark mark = null, HttpMethod methodType = null, string errors = null, string responseBodyRaw = null, string additionalInfo = null, object payload = null, [ CallerMemberName ] string libMethodName = null )
		{
			JObject responseBody = null;
			try
			{
				responseBody = JObject.Parse( responseBodyRaw );
			}
			catch { }

			string serviceEndPoint = null;
			string requestParameters = null;
			
			if ( !string.IsNullOrEmpty( url ) )
			{
				Uri uri = new Uri( url );
			
				serviceEndPoint = uri.LocalPath;
				requestParameters = uri.Query;
			}

			return new CallInfo()
			{
				Mark = mark?.ToString() ?? "Unknown",
				Endpoint = serviceEndPoint,
				RequestParameters = requestParameters,
				Method = methodType?.ToString() ?? "Uknown",
				Body = payload,
				LibMethodName = libMethodName,
				AdditionalInfo = additionalInfo,
				Response = (object)responseBody ?? responseBodyRaw,
				Errors = errors
			}.ToJson();
		}
	}
}
