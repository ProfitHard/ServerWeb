using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWeb.BLL.DTO;
using ServerWeb.BLL.Models;
using ServerWeb.DAL.Context;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ServerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessageController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Message?receiverId=5&page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedListResponse<MessageResponse>>> GetMessages(int? receiverId, int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var query = _context.Messages
               .Include(m => m.Sender)
               .Include(m => m.Receiver)
                 .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == userId))
              .OrderByDescending(m => m.SentAt);
            if (receiverId == null)
            {
                query = _context.Messages
               .Include(m => m.Sender)
               .Include(m => m.Receiver)
               .Where(m => m.SenderId == userId || m.ReceiverId == userId)
               .OrderByDescending(m => m.SentAt);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            if (messages == null || messages.Count == 0)
            {
                return NoContent();
            }
            var messageResponses = messages.Select(m => MapToResponse(m)).ToList();
            var response = new PagedListResponse<MessageResponse>
            {
                Items = messageResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
            return Ok(response);
        }

        // GET: api/Message/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MessageResponse>> GetMessage(int id)
        {
            var message = await _context.Messages
              .Include(m => m.Sender)
              .Include(m => m.Receiver)
              .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound($"Message with ID '{id}' not found.");
            }

            return Ok(MapToResponse(message));
        }

        // POST: api/Message
        [HttpPost]
        public async Task<ActionResult<MessageResponse>> PostMessage([FromBody] MessageCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Unauthorized("User not found, try to re-authenticate.");
            }
            var receiver = await _context.Users.FindAsync(request.ReceiverId);
            if (receiver == null)
            {
                return BadRequest("Invalid ReceiverId");
            }
            var message = new Message
            {
                Content = request.Content,
                SentAt = DateTime.Now,
                SenderId = user.Id,
                ReceiverId = request.ReceiverId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, MapToResponse(message));
        }
        private MessageResponse MapToResponse(Message message)
        {
            return new MessageResponse
            {
                Id = message.Id,
                Content = message.Content,
                SentAt = message.SentAt,
                Sender = new UserResponse
                {
                    Id = message.Sender.Id,
                    UserName = message.Sender.Username
                },
                Receiver = new UserResponse
                {
                    Id = message.Receiver.Id,
                    UserName = message.Receiver.Username
                }

            };
        }
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
