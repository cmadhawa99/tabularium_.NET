// Services/AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArchivumWpf.Services;

/* 
Used only by `dotnet ef migrations add` / `dotnet ef database update` at design time.
Never used at runtime - the real app resolves connections via SessionContext.
The connection string here is never actually opened for `migrations add` (EF only
needs it to build the model shape), so a placeholder is fine.
*/

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time_only;Username=dummy_user;Password=dummy_pw");

        return new AppDbContext(optionsBuilder.Options);
    }
}