using CuttingEdge.Conditions;
using NetSuiteAccess.Configuration;

namespace NetSuiteAccess.Models.Commands
{
	public abstract class NetSuiteCommand
	{
		public NetSuiteConfig Config { get; private set; }
		public string RelativeUrl { get; private set; }

		public string Url { get; protected set; }
		public string AbsoluteUrl
		{
			get { return this.Config.ApiBaseUrl + this.Url; }
		}
		public string Payload { get; protected set; }

		protected NetSuiteCommand( NetSuiteConfig config, string relativeUrl )
		{
			Condition.Requires( config, "config" ).IsNotNull();

			this.Config = config;
			this.RelativeUrl = relativeUrl;
		}
	}
}