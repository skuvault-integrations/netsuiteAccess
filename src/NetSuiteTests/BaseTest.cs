using CsvHelper;
using CsvHelper.Configuration;
using NetSuiteAccess.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetSuiteTests
{
	public abstract class BaseTest
	{
		protected NetSuiteConfig Config { get; private set; }

		public BaseTest()
		{
			var testCredentials = this.LoadTestSettings< TestCredentials >( @"\..\..\credentials.csv" );
			this.Config = new NetSuiteConfig( new NetSuiteCredentials( testCredentials.CustomerId, testCredentials.ConsumerKey, testCredentials.ConsumerSecret, testCredentials.TokenId, testCredentials.TokenSecret ) );
		}

		protected T LoadTestSettings< T >( string filePath )
		{
			string basePath = new Uri( Path.GetDirectoryName( Assembly.GetExecutingAssembly().CodeBase ) ).LocalPath;

			using( var streamReader = new StreamReader( basePath + filePath ) )
			{
				var csvConfig = new Configuration()
				{
					Delimiter = ","
				};

				using( var csvReader = new CsvReader( streamReader, csvConfig ) )
				{
					var credentials = csvReader.GetRecords< T >();

					return credentials.FirstOrDefault();
				}
			}
		}
	}
}