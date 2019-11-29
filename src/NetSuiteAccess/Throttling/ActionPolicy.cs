using CuttingEdge.Conditions;
using NetSuiteAccess.Exceptions;
using Polly;
using System;
using System.Threading.Tasks;

namespace NetSuiteAccess.Throttling
{
	public class ActionPolicy
	{
		private readonly int _retryAttempts;
		private readonly int _delay;
		private readonly int _delayRate;

		public ActionPolicy( int attempts, int delay, int delayRate )
		{
			Condition.Requires( attempts ).IsGreaterThan( 0 );
			Condition.Requires( delay ).IsGreaterOrEqual( 0 );
			Condition.Requires( delayRate ).IsGreaterOrEqual( 0 );

			this._retryAttempts = attempts;
			this._delay = delay;
			this._delayRate = delayRate;
		}

		/// <summary>
		///	Retries function until it succeed or failed
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="funcToThrottle"></param>
		/// <param name="onRetryAttempt">Retry attempts</param>
		/// <param name="extraLogInfo"></param>
		/// <param name="onException"></param>
		/// <returns></returns>
		public Task< TResult > ExecuteAsync< TResult >( Func< Task< TResult > > funcToThrottle, Action< Exception, TimeSpan, int > onRetryAttempt, Func< string > extraLogInfo, Action< Exception > onException )
		{
			return Policy.Handle< NetSuiteNetworkException >()
				.WaitAndRetryAsync( _retryAttempts,
					retryCount => TimeSpan.FromSeconds( GetDelayBeforeNextAttempt( retryCount ) ),
					( exception, timeSpan, retryCount, context ) =>
					{
						onRetryAttempt?.Invoke( exception, timeSpan, retryCount );
					})
				.ExecuteAsync( async () =>
				{
					return await funcToThrottle().ConfigureAwait( false );
				});
		}

		public int GetDelayBeforeNextAttempt( int retryCount )
		{
			return this._delay + this._delayRate * retryCount;
		}
	}
}