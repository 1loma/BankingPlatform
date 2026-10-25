using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Api.Data
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options): base(options)
        {}



    }
}