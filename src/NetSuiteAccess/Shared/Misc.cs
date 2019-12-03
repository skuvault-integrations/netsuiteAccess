using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NetSuiteAccess.Shared
{
	public static class Misc
	{
		public static string ToJson( this object source )
		{
			try
			{
				if (source == null)
					return "{}";
				else
				{
					var serialized = JsonConvert.SerializeObject( source, new IsoDateTimeConverter() );
					return serialized;
				}
			}
			catch( Exception )
			{
				return "{}";
			}
		}

		/// <summary>
		///	Custom implementation of URI components encoding for RFC 5849
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string EscapeUriData( string data )
		{
			string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
			StringBuilder result = new StringBuilder();

			foreach ( char symbol in data ) {
				if ( unreservedChars.IndexOf(symbol) != -1 ) {
					result.Append( symbol );
				} else {
					result.Append('%' + String.Format("{0:X2}", (int)symbol));
				}
			}

			return result.ToString();
		}

		/// <summary>
		///	Parses url query string into dictionary
		/// </summary>
		/// <param name="queryParams">Query parameters</param>
		/// <returns></returns>
		public static Dictionary< string, string > ParseQueryParams( string queryParams )
		{
			var result = new Dictionary< string, string >();

			if ( !string.IsNullOrEmpty( queryParams ) )
			{
				string[] keyValuePairs = queryParams.Replace( "?", "" ).Split( '&' );

				foreach ( string keyValuePair in keyValuePairs )
				{
					string[] keyValue = keyValuePair.Split( '=' );

					if ( keyValue.Length == 2 )
					{
						if ( !result.TryGetValue( keyValue[0], out var tmp ) )
							result.Add( keyValue[0], keyValue[1] );
					}
				}
			}

			return result;
		}

		public static string FromUtcToRFC3339( this DateTime dateTimeUtc )
		{
			return XmlConvert.ToString( dateTimeUtc, XmlDateTimeSerializationMode.Utc );
		}

		public static DateTime FromRFC3339ToUtc( this string rfc3339DateTime )
		{
			return XmlConvert.ToDateTime( rfc3339DateTime, XmlDateTimeSerializationMode.Utc );
		}
	}
}