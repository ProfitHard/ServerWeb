using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.Models
{
    public class PostCreateRequest
    {
        [Required]
        public string Content { get; set; }
    }
}
