using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerWeb.BLL.Models;
using System;


namespace ServerWeb.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<AudioRecord> AudioRecords { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Video> Videos { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка связей между сущностями

            // User - AudioRecord (один ко многим)

            modelBuilder.Entity<User>()
                .HasMany<AudioRecord>(u => u.UploadedAudioRecords)
                .WithOne(ar => ar.Uploader)
                .HasForeignKey(ar => ar.UserId);

            // AudioRecord - Comment (один ко многим)
            modelBuilder.Entity<AudioRecord>()
                 .HasMany<Comment>(ar => ar.Comments)
                 .WithOne(c => c.AudioRecord)
                 .HasForeignKey(c => c.AudioRecordId);
            // User - Comment (один ко многим)
            modelBuilder.Entity<User>()
                .HasMany<Comment>(u => u.Comments)
                .WithOne(c => c.Author)
                .HasForeignKey(c => c.UserId);

            // User - FriendRequest (отправитель запроса)
            modelBuilder.Entity<User>()
                .HasMany(u => u.FriendRequestsSent)
                .WithOne(fr => fr.Sender)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем каскадное удаление отправителя


            // User - FriendRequest (получатель запроса)
            modelBuilder.Entity<User>()
               .HasMany(u => u.FriendRequestsReceived)
               .WithOne(fr => fr.Receiver)
               .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем каскадное удаление получателя

            // User - User (многие ко многим - друзья)
            modelBuilder.Entity<User>()
             .HasMany(u => u.Friends)
            .WithMany()
              .UsingEntity(j => j.ToTable("Friends"));

            // AudioRecord - Like (один ко многим)
            modelBuilder.Entity<AudioRecord>()
                .HasMany(ar => ar.Likes)
                .WithOne(l => l.AudioRecord)
                .HasForeignKey(l => l.AudioRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Like (один ко многим)
            modelBuilder.Entity<User>()
                 .HasMany(u => u.Likes)
                .WithOne(l => l.User)
                 .HasForeignKey(l => l.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            // User - Post (один ко многим)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.Author)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Post - Comment (один ко многим)
            modelBuilder.Entity<Post>()
                 .HasMany<Comment>(p => p.Comments)
                .WithOne(c => c.Post)
                 .HasForeignKey(c => c.PostId)
                 .OnDelete(DeleteBehavior.Cascade);

            // User - Video (один ко многим)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UploadedVideos)
                .WithOne(v => v.Uploader)
                .HasForeignKey(v => v.UserId)
                 .OnDelete(DeleteBehavior.Cascade);


            ConfigureEntities(modelBuilder);
        }

        private void ConfigureEntities(ModelBuilder modelBuilder)
        {
            //Configure User
            modelBuilder.Entity<User>(ConfigureUserEntity);
        }
        private void ConfigureUserEntity(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(u => u.Username).IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("YourConnectionString");
        }
    }
}
