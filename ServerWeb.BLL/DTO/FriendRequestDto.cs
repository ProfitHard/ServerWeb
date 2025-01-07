using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.DTO
{
    public class FriendRequestDto
    {
        [Required]
        public int ReceiverId { get; set; }
    }
}
