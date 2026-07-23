using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Pandacap.Local
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context) => true;
    }
}
