using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pandacap.Data
{
    public class PandacapIdentityDbContext(DbContextOptions<PandacapIdentityDbContext> options) : IdentityDbContext(options)
    {

    }
}
