using NetSuiteAccess.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NetSuiteAccess.Services.Customers
{
	public interface INetSuiteCustomersService
	{
		Task< NetSuiteCustomer > GetCustomerInfoByIdAsync( long customerId, CancellationToken token );
		Task< NetSuiteCustomer > GetCustomerInfoByEmailAsync( string email, CancellationToken token );
	}
}