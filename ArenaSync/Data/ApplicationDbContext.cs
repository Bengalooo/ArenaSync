using ArenaSync.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Turniej> Turnieje { get; set; } = null!;
        public DbSet<Druzyna> Druzyny { get; set; } = null!;
        public DbSet<Zawodnik> Zawodnicy { get; set; } = null!;
        public DbSet<Mecz> Mecze { get; set; } = null!;
        public DbSet<Uwaga> Uwagi { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Druzyna>()
                .HasOne(d => d.Turniej).WithMany(t => t.Druzyny)
                .HasForeignKey(d => d.TurniejId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Zawodnik>()
                .HasOne(z => z.Druzyna).WithMany(d => d.Zawodnicy)
                .HasForeignKey(z => z.DruzynaId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Mecz>()
                .HasOne(m => m.Turniej).WithMany(t => t.Mecze)
                .HasForeignKey(m => m.TurniejId).OnDelete(DeleteBehavior.Cascade);

            // Dwie relacje do tej samej tabeli Druzyny - bez kaskady, żeby uniknąć wielu ścieżek usuwania
            builder.Entity<Mecz>()
                .HasOne(m => m.Gospodarz).WithMany()
                .HasForeignKey(m => m.GospodarzId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Mecz>()
                .HasOne(m => m.Gosc).WithMany()
                .HasForeignKey(m => m.GoscId).OnDelete(DeleteBehavior.Restrict);

            // Usunięcie drużyny nie usuwa konta zawodnika, tylko odpina go od drużyny
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Druzyna).WithMany()
                .HasForeignKey(u => u.DruzynaId).OnDelete(DeleteBehavior.SetNull);

            // Usunięcie konta usuwa też jego zgłoszone uwagi
            builder.Entity<Uwaga>()
                .HasOne(u => u.Autor).WithMany()
                .HasForeignKey(u => u.AutorId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Zawodnik>().Ignore(z => z.PelneImie);
            builder.Entity<Mecz>().Ignore(m => m.Rozegrany);
        }
    }
}
