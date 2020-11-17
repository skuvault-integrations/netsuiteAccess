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
		public void GivenPageWithDefaultSize_WhenGetHalfPageCalled_ThenHalfPageSizeIsReturned()
		{
			var currentPageSize = DefaultPageSize;
			var newPageSize = PageAdjuster.GetHalfPageSize( currentPageSize );
			newPageSize.Should().Be( DefaultPageSize / 2 );
		}

		[ Test ]
		public void GivenPageWithMinPage_WhenGetHalfPageCalled_ThenSamePageSizeIsReturned()
		{
			var currentPageSize = 1;
			var newPageSize = PageAdjuster.GetHalfPageSize( currentPageSize );
			newPageSize.Should().Be( currentPageSize );
		}

		[ Test ]
		public void GivenFirstPageWithHalfPageExpected_WhenGetNextPageIndexCalled_ThenNextPageIndexIsTheSame()
		{
			var firstPageIndex = 1;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( firstPageIndex, DefaultPageSize ), 125 );
			newPageIndex.Should().Be( firstPageIndex );
		}

		[ Test ]
		public void GivenPageWithHalfPageExpected_WhenGetNextPageIndexCalled_ThenNextPageIndexIsRecalculatedCorrectly()
		{
			var currentPageIndex = 5;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( currentPageIndex, DefaultPageSize ), 125 );
			newPageIndex.Should().Be( currentPageIndex * 2 - 1 );
		}

		[ Test ]
		public void GivenPageWithHalfPageExpectedAndNotDefaultCurrentPage_WhenGetNextPageIndexCalled_ThenNextPageIndexIsRecalculatedCorrectly()
		{
			var currentPageIndex = 5;
			var currentPageSize = 125;
			var newPageIndex = PageAdjuster.GetNextPageIndex( new PageInfo( currentPageIndex, currentPageSize ), 62 );
			newPageIndex.Should().Be( currentPageIndex * 2 - 1 );
		}
	}
}