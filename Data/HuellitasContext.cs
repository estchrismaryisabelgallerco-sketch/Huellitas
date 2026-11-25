
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Huellitas.Models;

namespace Huellitas.Data
{
    public class HuellitasContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public HuellitasContext(DbContextOptions<HuellitasContext> options)
            : base(options)
        {
        }

        public DbSet<Propietario> Propietarios { get; set; }
        public DbSet<Mascota> Mascotas { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Consulta> Consultas { get; set; }

        public DbSet<Personal> Personal { get; set; }
        public DbSet<HorarioPersonal> HorariosPersonal { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Mascota>()
                .HasOne(m => m.Propietario)
                .WithMany(p => p.Mascotas)
                .HasForeignKey(m => m.PropietarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mascota>()
                .Property(m => m.Sexo)
                .HasColumnType("enum('Macho','Hembra')");

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Mascota)
                .WithMany()
                .HasForeignKey(c => c.MascotaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Personal)
                .WithMany(p => p.Citas)
                .HasForeignKey(c => c.PersonalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cita>()
                .Property(c => c.Estado)
                .HasColumnType("enum('Pendiente','Confirmada','Atendida','Cancelada')");

            // Relación Mascota - Consulta
            modelBuilder.Entity<Consulta>()
                .HasOne(c => c.Mascota)
                .WithMany()
                .HasForeignKey(c => c.MascotaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Cita - Consulta
            modelBuilder.Entity<Consulta>()
                .HasOne(c => c.Cita)
                .WithOne()
                .HasForeignKey<Consulta>(c => c.CitaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación IdentityUser - Personal (uno a uno)
            modelBuilder.Entity<Personal>()
                .HasOne(p => p.Usuario)
                .WithOne()
                .HasForeignKey<Personal>(p => p.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Personal>()
                .HasIndex(p => p.IdentityUserId)
                .IsUnique();

            // Relación Personal - HorariosPersonal (uno a muchos)
            modelBuilder.Entity<HorarioPersonal>()
                .HasOne(h => h.Personal)
                .WithMany(p => p.Horarios)
                .HasForeignKey(h => h.PersonalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HorarioPersonal>()
                .ToTable("HorariosPersonal");

            modelBuilder.Entity<Personal>()
                .ToTable("Personal");
        }
    }
}