using System;
using System.ComponentModel.DataAnnotations;

namespace ServerWeb.BLL.DTO
{
    // DTO для обновления комментария
    public class CommentUpdateRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
