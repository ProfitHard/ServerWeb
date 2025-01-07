using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace ServerWeb.BLL.Models
{
    public class AudioRecord
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Artist { get; set; }
        public string FilePath { get; set; } // Путь к файлу
        public DateTime UploadDate { get; set; }

        public int UserId { get; set; } // Внешний ключ для пользователя
        [ForeignKey("UserId")]
        public User Uploader { get; set; } // Навигационное свойство для связи с пользователем
                                           // Навигационное свойство для связи с комментариями
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        // Навигационное свойство для лайков
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}