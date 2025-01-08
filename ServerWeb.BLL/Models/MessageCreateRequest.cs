using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.Models
{
    public class MessageCreateRequest
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public int ReceiverId { get; set; }

    }
}
