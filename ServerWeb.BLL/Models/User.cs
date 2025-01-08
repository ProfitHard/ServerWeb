using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public ICollection<AudioRecord> UploadedAudioRecords { get; set; } // Добавлено

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public ICollection<FriendRequest> FriendRequestsSent { get; set; } = new List<FriendRequest>();
        public ICollection<FriendRequest> FriendRequestsReceived { get; set; } = new List<FriendRequest>();
        public ICollection<User> Friends { get; set; } = new List<User>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Video> UploadedVideos { get; set; } = new List<Video>();
        [Required]
        public string PasswordHash { get; set; }
        public ICollection<Message> SentMessages { get; set; } = new List<Message>(); // Отправленные сообщения
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>(); // Полученные сообщения

    }
}
