using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using CuttingEdge.Conditions;

namespace NetSuiteAccess.Shared
{
	public class OAuthenticator
	{
		private readonly string _realmId;
		private readonly string _consumerKey;
		private readonly string _consumerSecret;
		private readonly string _token;
		private readonly string _tokenSecret;

		public OAuthenticator( string realmId, string consumerKey, string consumerSecret, string token, string tokenSecret )
		{
			Condition.Requires( realmId ).IsNotNullOrWhiteSpace();
			Condition.Requires( consumerKey ).IsNotNullOrWhiteSpace();
			Condition.Requires( consumerSecret ).IsNotNullOrWhiteSpace();
			Condition.Requires( token ).IsNotNullOrWhiteSpace();
			Condition.Requires( tokenSecret ).IsNotNullOrWhiteSpace();

			this._realmId = realmId;
			this._consumerKey = consumerKey;
			this._consumerSecret = consumerSecret;
			this._token = token;
			this._tokenSecret = tokenSecret;
		}

		/// <summary>
		///	Returns url with OAuth 1.0 query parameters
		/// </summary>
		/// <param name="url"></param>
		/// <param name="methodName"></param>
		/// <param name="extraRequestParameters"></param>
		/// <returns></returns>
		public string GetAuthorizationHeader( string url, HttpMethod httpMethod, Dictionary< string, string > extraRequestParameters = null )
		{
			var oauthRequestParams = GetOAuthRequestParameters( url, httpMethod, extraRequestParameters );

			return GetAuthorizationHeader( oauthRequestParams );
		}

		/// <summary>
		///	Returns OAuth 1.0 request parameters with signature
		/// </summary>
		/// <param name="url"></param>
		/// <param name="httpMethod"></param>
		/// <param name="tokenSecret"></param>
		/// <param name="extraRequestParameters"></param>
		/// <returns></returns>
		public Dictionary< string, string > GetOAuthRequestParameters( string url, HttpMethod httpMethod, Dictionary< string, string > extraRequestParameters )
		{
			// standard OAuth 1.0 request parameters
			var requestParameters = new Dictionary< string, string >
			{
				{ "oauth_consumer_key", this._consumerKey },
				{ "oauth_nonce", GetRandomSessionNonce() },
				{ "oauth_signature_method", "HMAC-SHA1" },
				{ "oauth_timestamp", GetUtcEpochTime().ToString() },
				{ "oauth_version", "1.0" },
			};

			// if request token exists
			if ( !string.IsNullOrEmpty( this._token ) )
				requestParameters.Add("oauth_token", this._token);

			// extra query parameters
			if ( extraRequestParameters != null )
			{
				foreach( var keyValue in extraRequestParameters ) {
					if ( !requestParameters.ContainsKey( keyValue.Key ) )
					{
						requestParameters.Add( keyValue.Key, keyValue.Value );
					} 
					else
					{
						requestParameters[ keyValue.Key ] = keyValue.Value;
					}
				}
			}

			Uri uri = new Uri( url );
			string baseUrl = uri.Scheme + "://" + uri.Host + uri.LocalPath;

			// extra parameters can be placed also directly in the url
			var queryParams = Misc.ParseQueryParams( uri.Query );

			foreach ( var queryParam in queryParams )
			{
				if ( !requestParameters.ContainsKey( queryParam.Key ) )
					requestParameters.Add( queryParam.Key, Uri.UnescapeDataString( queryParams[ queryParam.Key ] ) );
			}

			requestParameters.Remove( "oauth_signature" );

			string signature = GetOAuthSignature( baseUrl, httpMethod.ToString().ToUpper(), requestParameters );
			requestParameters.Add( "oauth_signature", signature );

			// if http method isn't GET all request parameters should be included in the request body
			if ( extraRequestParameters != null
			    && !httpMethod.ToString().ToUpper().Equals( "GET" ) )
			{
				foreach ( var keyValue in extraRequestParameters )
					requestParameters.Remove( keyValue.Key );
			}

			return requestParameters;
		}

		/// <summary>
		///	Returns signed request payload by using HMAC-SHA1
		/// </summary>
		/// <param name="url"></param>
		/// <param name="urlMethod"></param>
		/// <param name="tokenSecret"></param>
		/// <param name="requestParameters"></param>
		/// <returns></returns>
		private string GetOAuthSignature( string baseUrl, string urlMethod, Dictionary< string, string > requestParameters )
		{
			string signature = null;

			string urlEncoded = EscapeUriData( baseUrl );
			string encodedParameters = EscapeUriData( string.Join( "&",
				requestParameters.OrderBy( kv => kv.Key ).Select( item =>
					($"{ EscapeUriData( item.Key ) }={ EscapeUriData( item.Value ) }") ) ) );
			
			string baseString = $"{ urlMethod.ToUpper() }&{ urlEncoded }&{ encodedParameters }";

			HMACSHA1 hmacsha1 = new HMACSHA1( Encoding.ASCII.GetBytes( this._consumerSecret + "&" + ( string.IsNullOrEmpty( this._tokenSecret ) ? "" : this._tokenSecret) ) );
			byte[] data = Encoding.ASCII.GetBytes( baseString );

			using (var stream = new MemoryStream( data ))
				signature = Convert.ToBase64String( hmacsha1.ComputeHash( stream ) );

			return signature;
		}

		/// <summary>
		///	Generates random nonce for each request
		/// </summary>
		/// <returns></returns>
		private string GetRandomSessionNonce()
		{
			return Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 11 ).ToUpper();
		}

		/// <summary>
		///	Returns url with query parameters
		/// </summary>
		/// <param name="url"></param>
		/// <param name="requestParameters"></param>
		/// <returns></returns>
		public string GetAuthorizationHeader( Dictionary< string, string > requestParameters )
		{
			var headerBuilder = new StringBuilder();
			headerBuilder.Append( string.Format("realm=\"{0}\",", this._realmId.ToUpper() ) );

			foreach ( var kv in requestParameters )
			{
				if ( kv.Key.StartsWith( "oauth" ) )
				{
					headerBuilder.Append( String.Format( "{0}=\"{1}\",", kv.Key, Misc.EscapeUriData( kv.Value ) ) );
				}
			}

			return string.Format( "OAuth {0}", headerBuilder.ToString().TrimEnd(',') );
		}

		/// <summary>
		///	Custom implementation of URI components encoding for RFC 5849
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private string EscapeUriData( string data )
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
		///	Returns Unix epoch (number of seconds elapsed since January 1, 1970)
		/// </summary>
		/// <returns></returns>
		private long GetUtcEpochTime()
		{
			return (int)( DateTime.UtcNow - new DateTime( 1970, 1, 1 ) ).TotalSeconds;
		}
	}
}
