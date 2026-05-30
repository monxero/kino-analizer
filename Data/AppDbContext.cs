using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KinoAnalyzer.Models;

namespace KinoAnalyzer.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Sorteo> Sorteos { get; set; }
    public DbSet<NumeroSorteado> NumerosSorteados { get; set; }
    public DbSet<CombinacionUsuario> CombinacionesUsuario { get; set; }
    public DbSet<NumeroCombinacion> NumerosCombinacion { get; set; }
    public DbSet<LogScraper> LogsScraper { get; set; }
}