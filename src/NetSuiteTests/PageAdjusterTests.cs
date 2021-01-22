using FluentAssertions;
using NetSuiteAccess.Shared;
using NUnit.Framework;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class PageAdjusterTests
	{
		private const int DefaultPageSize = 250;
		private const int MinPageSize = 1;

		[ Test ]
		public void GivenPageWithDefaultPageSize_WhenGetHalfPageSizeIsCalled_ThenHalfPageSizeIsReturned()
		{
			var currentPageSize = DefaultPageSize;
			var newPageSize = PageAdjuster.GetHalfPageSize( currentPageSize );
			newPageSize.Should().Be( DefaultPageSize / 2 );
		}

		[ Test ]
		public void GivenPageWithMinPageSize_WhenGetHalfPageSizeIsCalled_ThenSamePageSizeIsReturned()
		{
			var currentPageSize = 1;
			var newPageSize = PageAdjuster.GetHalfPageSize( currentPageSize );
			newPageSize.Should().Be( currentPageSize );
		}

		[ Test ]
		public void GivenPageWithFirstIndex_WhenGetNextPageIndexWithDecreasedPageSizeTwiceIsCalled_ThenNextPageIndexIsTheSame()
		{
			var firstPageIndex = 1;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( firstPageIndex, DefaultPageSize ), 125 );
			newPageIndex.Should().Be( firstPageIndex );
		}

		[ Test ]
		public void GivenPageWithIndexHigherThanFirst_WhenGetNextPageIndexWithDecreasedPageSizeTwiceIsCalled_ThenNextPageIndexIsRecalculatedCorrectly()
		{
			var currentPageIndex = 5;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( currentPageIndex, DefaultPageSize ), 125 );
			newPageIndex.Should().Be( currentPageIndex * 2 - 1 );
		}

		[ Test ]
		public void GivenPageWithDecreasedTwiceDefaultPageSize_WhenGetNextPageIndexWithDecreasedPageSizeTwiceIsCalled_ThenNextPageIndexIsCalculatedCorrectly()
		{
			var currentPageIndex = 5;
			var currentPageSize = DefaultPageSize / 2;
			var halfPageSize = currentPageSize / 2;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( currentPageIndex, currentPageSize ), halfPageSize );
			newPageIndex.Should().Be( currentPageIndex * 2 - 1 );
		}
	}
}