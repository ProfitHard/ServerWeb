using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWeb.BLL.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User Author { get; set; }

        public int AudioRecordId { get; set; }  // Связь с аудиозаписью
        [ForeignKey("AudioRecordId")]
        public AudioRecord AudioRecord { get; set; }
        public int? PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; }
    }
}
