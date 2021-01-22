using System;

namespace NetSuiteAccess.Shared
{
	public class PageAdjuster
	{
		public static int GetHalfPageSize( int currentPageSize )
		{
			return Math.Max( (int)Math.Floor( currentPageSize / 2d ), 1 );
		}

		public static int GetNextPageIndex( PageInfo currentPageInfo, int newPageSize )
		{
			var entitiesReceived = currentPageInfo.Size * ( currentPageInfo.Index - 1 );
			return (int)Math.Floor( entitiesReceived * 1.0 / newPageSize ) + 1;
		}
	}

	public struct PageInfo
	{
		public PageInfo( int index, int size )
		{
			this.Index = index;
			this.Size = size;
		}

		public int Index { get; set; }
		public int Size { get; set; }
	}
}