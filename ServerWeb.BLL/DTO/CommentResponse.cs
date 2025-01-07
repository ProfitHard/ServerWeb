using ServerWeb.BLL.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ServerWeb.BLL.DTO
{
    // DTO для ответа на запрос комментария
    public class CommentResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserResponse Author { get; set; }
    }
}
