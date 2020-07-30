using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NetSuiteAccess.Models
{
	public class NetSuiteLocation
	{
		[ JsonProperty( "id" ) ]
		public long Id { get; set; }
		[ JsonProperty( "name" ) ]
		public string Name { get; set; }

		public bool UseBins { get; set; }
		public IEnumerable< string > Subsidiaries { get; set; }
	}

	public static class LocationExtensions
	{
		public static NetSuiteLocation ToSVLocation( this NetSuiteSoapWS.Location location )
		{
			return new NetSuiteLocation
			{
				Id = long.Parse( location.internalId ),
				Name = location.name,
				UseBins = location.useBins,
				Subsidiaries = location.subsidiaryList?.Select( x => x.internalId ) ?? new List< string >()
			};
		}
	}
}