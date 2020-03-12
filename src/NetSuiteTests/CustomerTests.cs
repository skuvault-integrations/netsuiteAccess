using FluentAssertions;
using NetSuiteAccess.Services.Customers;
using NUnit.Framework;
using System.Threading;

namespace NetSuiteTests
{
	[ TestFixture ]
	public class CustomerTests : BaseTest
	{
		private INetSuiteCustomersService _customersService;
		private long _individualCustomerId = 1680;
		private long _companyCustomerId = 1100;

		[ SetUp ]
		public void Init()
		{
			this._customersService = new NetSuiteCustomersService( base.Config );
		}

		[ Test ]
		public void GetIndividualCustomerInfo()
		{
			var customerInfo = this._customersService.GetCustomerInfoByIdAsync( this._individualCustomerId, CancellationToken.None ).Result;

			customerInfo.Should().NotBeNull();
			customerInfo.FirstName.Should().NotBeNullOrWhiteSpace();
			customerInfo.LastName.Should().NotBeNullOrWhiteSpace();
			customerInfo.Phone.Should().NotBeNullOrWhiteSpace();
			customerInfo.Email.Should().NotBeNullOrWhiteSpace();
		}

		[ Test ]
		public void GetCompanyCustomerInfo()
		{
			var customerInfo = this._customersService.GetCustomerInfoByIdAsync( this._companyCustomerId, CancellationToken.None ).Result;

			customerInfo.Should().NotBeNull();
			customerInfo.CompanyName.Should().NotBeNullOrWhiteSpace();
			customerInfo.FirstName.Should().BeNullOrWhiteSpace();
			customerInfo.LastName.Should().BeNullOrWhiteSpace();
			customerInfo.Phone.Should().NotBeNullOrWhiteSpace();
			customerInfo.Email.Should().NotBeNullOrWhiteSpace();
		}
	}
}