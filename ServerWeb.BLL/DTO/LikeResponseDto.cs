using ServerWeb.BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.DTO
{
    public class LikeResponseDto
    {
        public int Id { get; set; }
        public UserResponse User { get; set; }
        public DateTime LikedAt { get; set; }
    }
}
