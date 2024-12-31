using Microsoft.EntityFrameworkCore;
using ServerWeb.BLL.Models;
using System;


namespace ServerWeb.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<AudioRecord> AudioRecords { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany<AudioRecord>(u => u.UploadedAudioRecords)
                .WithOne(ar => ar.Uploader)
                .HasForeignKey(ar => ar.UserId);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("YourConnectionString");
        }
    }
}
