using ServerWeb.BLL.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.Models
{
    public class FriendRequestResponse
    {
        public int Id { get; set; }
        public UserResponse Sender { get; set; }
        public UserResponse Receiver { get; set; }
        public DateTime RequestDate { get; set; }
        public bool IsAccepted { get; set; }
    }
}
