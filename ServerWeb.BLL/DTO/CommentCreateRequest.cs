using System;
using System.ComponentModel.DataAnnotations;

namespace ServerWeb.BLL.DTO
{
    // DTO для создания комментария
    public class CommentCreateRequest
    {
        [Required]
        public string Content { get; set; }
        [Required]
        public int AudioRecordId { get; set; } //  Id аудиозаписи к которой привязан комментарий
    }
}
