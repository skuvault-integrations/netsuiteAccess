using System.Collections.Generic;
using System.Linq;

namespace NetSuiteAccess.Shared
{
	public static class IEnumerableExtensions
	{
		public static IEnumerable< IEnumerable< TSource > > SplitToPieces< TSource >( this IEnumerable< TSource > source, int pieceSize )
		{
			var res = new List< IEnumerable< TSource > >();
			var sourceArray = source as TSource[] ?? source.ToArray();
			for( var c = 0; c < sourceArray.Count(); c += pieceSize )
			{
				res.Add( sourceArray.Skip( c ).Take( pieceSize ) );
			}

			return res;
		}
	}
}
