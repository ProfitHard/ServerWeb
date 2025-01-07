using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWeb.BLL.DTO
{
    public class LikeRequestDto
    {
        [Required]
        public int AudioRecordId { get; set; }
    }
}
