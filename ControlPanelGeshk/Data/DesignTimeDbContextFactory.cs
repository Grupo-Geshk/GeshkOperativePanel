// Data/DesignTimeDbContextFactory.cs
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControlPanelGeshk.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Carga .env cuando corre "dotnet ef ..."
        try { Env.Load(); } catch { /* no-op */ }

        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings__Postgres en el entorno/.env para tareas de diseño (dotnet ef).");

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new ApplicationDbContext(opts);
    }
}
