using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWeb.BLL.DTO;
using ServerWeb.BLL.Models;
using ServerWeb.DAL.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ServerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FriendController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Friend/request
        [HttpPost("request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDto requestDto)
        {
            var senderId = GetCurrentUserId();
            if (senderId == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null)
            {
                return Unauthorized("User not found.");
            }

            var receiver = await _context.Users.FindAsync(requestDto.ReceiverId);
            if (receiver == null)
            {
                return NotFound($"User with ID '{requestDto.ReceiverId}' not found.");
            }

            if (senderId == requestDto.ReceiverId)
            {
                return BadRequest("You can not send request to yourself");
            }
            var existingRequest = await _context.FriendRequests.FirstOrDefaultAsync(
                 fr => (fr.SenderId == senderId && fr.ReceiverId == requestDto.ReceiverId) || (fr.SenderId == requestDto.ReceiverId && fr.ReceiverId == senderId)
             );
            if (existingRequest != null)
            {
                return BadRequest("Friend request already exist");
            }

            var friendRequest = new FriendRequest
            {
                SenderId = senderId.Value,
                ReceiverId = requestDto.ReceiverId,
                RequestDate = DateTime.Now,
                IsAccepted = false,
            };

            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();
            return Ok("Friend Request sent");
        }

        // POST: api/Friend/accept/5
        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptFriendRequest(int requestId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }

            var friendRequest = await _context.FriendRequests
               .Include(fr => fr.Sender)
               .Include(fr => fr.Receiver)
                .FirstOrDefaultAsync(fr => fr.Id == requestId);

            if (friendRequest == null)
            {
                return NotFound($"Friend request with id '{requestId}' not found");
            }
            if (friendRequest.ReceiverId != userId)
            {
                return Forbid("You are not allowed to accept this request");
            }

            if (friendRequest.IsAccepted)
            {
                return BadRequest("Friend Request already accepted");
            }

            friendRequest.IsAccepted = true;

            var sender = await _context.Users.FindAsync(friendRequest.SenderId);
            var receiver = await _context.Users.FindAsync(friendRequest.ReceiverId);

            if (sender != null && receiver != null)
            {
                sender.Friends.Add(receiver);
                receiver.Friends.Add(sender);
            }

            await _context.SaveChangesAsync();

            return Ok("Friend request accepted");
        }

        // DELETE: api/Friend/5
        [HttpDelete("{requestId}")]
        public async Task<IActionResult> DeleteFriendRequest(int requestId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var friendRequest = await _context.FriendRequests
               .Include(fr => fr.Sender)
               .Include(fr => fr.Receiver)
               .FirstOrDefaultAsync(fr => fr.Id == requestId);
            if (friendRequest == null)
            {
                return NotFound($"Friend request with id '{requestId}' not found");
            }
            if (friendRequest.ReceiverId != userId && friendRequest.SenderId != userId)
            {
                return Forbid("You are not allowed to delete this request");
            }

            _context.FriendRequests.Remove(friendRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpDelete("remove/{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _context.Users
            .Include(u => u.Friends)
           .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var friend = await _context.Users
            .Include(u => u.Friends)
            .FirstOrDefaultAsync(u => u.Id == friendId);
            if (friend == null)
            {
                return NotFound($"Friend with id '{friendId}' not found");
            }
            if (user.Friends.All(f => f.Id != friendId))
            {
                return BadRequest("You are not friends");
            }
            user.Friends.Remove(friend);
            friend.Friends.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("friends")]
        public async Task<ActionResult<FriendListResponse>> GetFriends()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _context.Users
             .Include(u => u.Friends)
            .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }
            var friendListResponse = new FriendListResponse();
            foreach (var friend in user.Friends)
            {
                friendListResponse.Friends.Add(new UserResponse
                {
                    Id = friend.Id,
                    UserName = friend.Username
                });
            }
            return Ok(friendListResponse);
        }

        [HttpGet("requests")]
        public async Task<ActionResult<PagedListResponse<FriendRequestResponse>>> GetFriendRequests(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var totalItems = await _context.FriendRequests.Where(x => x.ReceiverId == userId).CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var friendRequests = await _context.FriendRequests
                 .Where(x => x.ReceiverId == userId)
                 .Include(fr => fr.Sender)
                 .Include(fr => fr.Receiver)
                 .OrderByDescending(fr => fr.RequestDate)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

            if (friendRequests == null || friendRequests.Count == 0)
            {
                return NoContent();
            }
            var friendRequestResponses = friendRequests.Select(fr => MapToResponse(fr)).ToList();
            var response = new PagedListResponse<FriendRequestResponse>
            {
                Items = friendRequestResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return Ok(response);
        }

        private FriendRequestResponse MapToResponse(FriendRequest friendRequest)
        {
            return new FriendRequestResponse
            {
                Id = friendRequest.Id,
                Sender = new UserResponse
                {
                    Id = friendRequest.Sender.Id,
                    UserName = friendRequest.Sender.Username
                },
                Receiver = new UserResponse
                {
                    Id = friendRequest.Receiver.Id,
                    UserName = friendRequest.Receiver.Username
                },
                RequestDate = friendRequest.RequestDate,
                IsAccepted = friendRequest.IsAccepted
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
