using ServerWeb.BLL.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWeb.BLL.Models
{
    public class FriendRequest
    {
    [Key]
    public int Id { get; set; }

    [Required]
    public int SenderId { get; set; }
    [ForeignKey("SenderId")]
    public User Sender { get; set; }

    [Required]
    public int ReceiverId { get; set; }
    [ForeignKey("ReceiverId")]
    public User Receiver { get; set; }

    public DateTime RequestDate { get; set; }

    public bool IsAccepted { get; set; }
    }
}
