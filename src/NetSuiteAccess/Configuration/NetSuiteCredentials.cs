using CuttingEdge.Conditions;

namespace NetSuiteAccess.Configuration
{
	public class NetSuiteCredentials
	{
		public string CustomerId { get; private set; }
		public string ConsumerKey { get; private set; }
		public string ConsumerSecret { get; private set; }
		public string TokenId { get; private set; }
		public string TokenSecret { get; private set; }

		public NetSuiteCredentials( string customerId, string consumerKey, string consumerSecret, string tokenId, string tokenSecret )
		{
			Condition.Requires( customerId, "customerId" ).IsNotNullOrWhiteSpace();
			Condition.Requires( consumerKey, "consumerKey" ).IsNotNullOrWhiteSpace();
			Condition.Requires( consumerSecret, "consumerSecret" ).IsNotNullOrWhiteSpace();
			Condition.Requires( tokenId, "tokenId" ).IsNotNullOrWhiteSpace();
			Condition.Requires( tokenSecret, "tokenSecret" ).IsNotNullOrWhiteSpace();

			this.CustomerId = customerId;
			this.ConsumerKey = consumerKey;
			this.ConsumerSecret = consumerSecret;
			this.TokenId = tokenId;
			this.TokenSecret = tokenSecret;
		}
	}
}