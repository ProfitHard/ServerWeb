using ServerWeb.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.Models
{
    public class FriendListResponse
    {
        public List<UserResponse> Friends { get; set; } = new List<UserResponse>();
    }
}
