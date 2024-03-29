﻿using CuttingEdge.Conditions;
using Netco.Logging;
using NetSuiteAccess.Configuration;
using NetSuiteAccess.Exceptions;
using NetSuiteAccess.Models;
using NetSuiteAccess.Models.Commands;
using NetSuiteAccess.Shared;
using NetSuiteAccess.Shared.Logging;
using NetSuiteAccess.Throttling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services
{
	public abstract class BaseService
	{
		protected NetSuiteConfig Config { get; private set; }
		protected Throttler Throttler { get; private set; }
		protected HttpClient HttpClient { get; private set; }
		protected Func< string > _additionalLogInfo;
		private const int _tooManyRequestsHttpCode = 429;

		/// <summary>
		///	Extra logging information
		/// </summary>
		public Func< string > AdditionalLogInfo
		{
			get { return this._additionalLogInfo ?? ( () => string.Empty ); }
			set => _additionalLogInfo = value;
		}

		protected BaseService( NetSuiteConfig config )
		{
			Condition.Requires( config, "config" ).IsNotNull();

			this.Config = config;
			this.Throttler = new Throttler( config.ThrottlingOptions.MaxRequestsPerTimeInterval, config.ThrottlingOptions.TimeIntervalInSec, config.ThrottlingOptions.MaxRetryAttempts );

			HttpClient = new HttpClient()
			{
				BaseAddress = new Uri( Config.ApiBaseUrl ) 
			};
		}

		protected async Task< IEnumerable< long > > GetEntitiesIds( NetSuiteCommand command, int pageLimit, CancellationToken cancellationToken, Mark mark = null )
		{
			var entitiesIds = new List< long >();
			bool hasMorePages = true;
			var pageCommand = new GetRecordsPageCommand( this.Config, command, pageLimit, 0 );

			while ( hasMorePages )
			{
				var page = await this.GetAsync< RecordsPage >( pageCommand, cancellationToken, mark ).ConfigureAwait( false );
				hasMorePages = page.HasMore;

				entitiesIds.AddRange( page.Items.Select( i => i.Id ) );
				pageCommand = new GetRecordsPageCommand( this.Config, command, pageLimit, page.Offset + pageLimit );
			}

			return entitiesIds;
		}

		protected async Task< T > GetAsync< T >( NetSuiteCommand command, CancellationToken cancellationToken, Mark mark = null, [ CallerMemberName ] string methodName = "" )
		{
			if ( mark == null )
				mark = Mark.CreateNew();

			if ( cancellationToken.IsCancellationRequested )
			{
				var exceptionDetails = CallInfo.CreateInfo( command.Url, mark, additionalInfo: this.AdditionalLogInfo(), libMethodName: methodName, methodType: HttpMethod.Get );
				throw new NetSuiteException( string.Format( "{0}. Task was cancelled", exceptionDetails ) );
			}

			var responseContent = await this.ThrottleRequestAsync( command, mark, async ( token ) =>
			{
				this.SetOAuthHeader( command );
				var httpResponse = await HttpClient.GetAsync( command.Url ).ConfigureAwait( false );
				var content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait( false );

				ThrowIfError( httpResponse, content );

				return content;
			}, cancellationToken ).ConfigureAwait( false );

			var response = JsonConvert.DeserializeObject< T >( responseContent );

			return response;
		}

		private void SetOAuthHeader( NetSuiteCommand command )
		{
			this.HttpClient.DefaultRequestHeaders.Remove( HttpRequestHeader.Authorization.ToString() );
			var authenticator = new OAuthenticator( this.Config.Credentials.CustomerId, 
										  this.Config.Credentials.ConsumerKey, 
										  this.Config.Credentials.ConsumerSecret, 
										  this.Config.Credentials.TokenId, 
										  this.Config.Credentials.TokenSecret );

			this.HttpClient.DefaultRequestHeaders.Add( HttpRequestHeader.Authorization.ToString(), authenticator.GetAuthorizationHeader( command.AbsoluteUrl, HttpMethod.Get ) );
		}

		protected void ThrowIfError( HttpResponseMessage response, string message )
		{
			HttpStatusCode responseStatusCode = response.StatusCode;

			if ( response.IsSuccessStatusCode )
				return;

			if ( responseStatusCode == HttpStatusCode.Unauthorized )
			{
				throw new NetSuiteUnauthorizedException( message );
			}
			else if ( (int)responseStatusCode == _tooManyRequestsHttpCode )
			{
				throw new NetSuiteRateLimitsExceeded( message );
			}
			else if ( responseStatusCode == HttpStatusCode.BadRequest )
			{
				throw new NetSuiteResourceAccessException( message );
			}

			throw new NetSuiteNetworkException( message );
		}

		private Task< T > ThrottleRequestAsync< T >( NetSuiteCommand command, Mark mark, Func< CancellationToken, Task< T > > processor, CancellationToken token )
		{
			return Throttler.ExecuteAsync( () =>
			{
				return new ActionPolicy( Config.NetworkOptions.RetryAttempts, Config.NetworkOptions.DelayBetweenFailedRequestsInSec, Config.NetworkOptions.DelayFailRequestRate )
					.ExecuteAsync( async () =>
					{
						Misc.InitSecurityProtocol();

						using( var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource( token ) )
						{
							NetSuiteLogger.LogStarted( CallInfo.CreateInfo( command.AbsoluteUrl, mark, payload: command.Payload, additionalInfo: this.AdditionalLogInfo() ) );
							linkedTokenSource.CancelAfter( Config.NetworkOptions.RequestTimeoutMs );

							var result = await processor( linkedTokenSource.Token ).ConfigureAwait( false );

							NetSuiteLogger.LogEnd( CallInfo.CreateInfo( command.AbsoluteUrl, mark, responseBodyRaw: result.ToJson(), additionalInfo: this.AdditionalLogInfo() ) );

							return result;
						}
					}, 
					( exception, timeSpan, retryCount ) =>
					{
						string retryDetails = CallInfo.CreateInfo( command.AbsoluteUrl, mark, additionalInfo: this.AdditionalLogInfo() );
						NetSuiteLogger.LogTraceRetryStarted( timeSpan.Seconds, retryCount, retryDetails );
					},
					() => CallInfo.CreateInfo( command.Url, mark, additionalInfo: this.AdditionalLogInfo() ),
					NetSuiteLogger.LogTraceException );
			} );
		}
	}
}