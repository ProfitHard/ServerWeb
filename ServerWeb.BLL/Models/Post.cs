using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ServerWeb.BLL.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User Author { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
