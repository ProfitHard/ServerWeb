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
    public class LikeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LikeController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Like
        [HttpPost]
        public async Task<ActionResult> LikeAudioRecord([FromBody] LikeRequestDto likeRequestDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }
            var audioRecord = await _context.AudioRecords.FindAsync(likeRequestDto.AudioRecordId);
            if (audioRecord == null)
            {
                return NotFound($"Audio record with ID '{likeRequestDto.AudioRecordId}' not found.");
            }

            var existingLike = await _context.Likes
                 .FirstOrDefaultAsync(l => l.UserId == userId && l.AudioRecordId == likeRequestDto.AudioRecordId);

            if (existingLike != null)
            {
                return BadRequest("You already liked this audio record");
            }

            var like = new Like
            {
                UserId = userId.Value,
                AudioRecordId = likeRequestDto.AudioRecordId,
                LikedAt = DateTime.Now
            };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return Ok("Audio record liked");

        }

        // DELETE: api/Like
        [HttpDelete]
        public async Task<IActionResult> UnlikeAudioRecord([FromBody] LikeRequestDto likeRequestDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }

            var like = await _context.Likes
                 .FirstOrDefaultAsync(l => l.UserId == userId && l.AudioRecordId == likeRequestDto.AudioRecordId);

            if (like == null)
            {
                return NotFound($"Like for audio record '{likeRequestDto.AudioRecordId}' not found.");
            }

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        //GET: api/Like/audioRecord/5
        [HttpGet("audioRecord/{audioRecordId}")]
        public async Task<ActionResult<PagedListResponse<LikeResponseDto>>> GetLikesForAudioRecord(int audioRecordId, int page = 1, int pageSize = 10)
        {
            var audioRecord = await _context.AudioRecords.FindAsync(audioRecordId);
            if (audioRecord == null)
            {
                return NotFound($"Audio record with ID '{audioRecordId}' not found.");
            }
            var totalItems = await _context.Likes.Where(x => x.AudioRecordId == audioRecordId).CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var likes = await _context.Likes
                 .Where(x => x.AudioRecordId == audioRecordId)
                 .Include(x => x.User)
                 .OrderByDescending(x => x.LikedAt)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();
            if (likes == null || likes.Count == 0)
            {
                return NoContent();
            }
            var likeResponse = likes.Select(l => MapToResponse(l)).ToList();
            var response = new PagedListResponse<LikeResponseDto>
            {
                Items = likeResponse,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
            return Ok(response);
        }
        //GET: api/Like/audioRecord/
        [HttpGet("audioRecord")]
        public async Task<ActionResult<PagedListResponse<AudioRecordWithLikeCountResponse>>> GetAudioRecordsWithLikeCounts(int page = 1, int pageSize = 10)
        {
            var totalItems = await _context.AudioRecords.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var audioRecords = await _context.AudioRecords
                .Include(a => a.Uploader)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            if (audioRecords == null || audioRecords.Count == 0)
            {
                return NoContent();
            }
            var audioRecordResponses = audioRecords.Select(ar => MapToResponseWithLike(ar)).ToList();

            var response = new PagedListResponse<AudioRecordWithLikeCountResponse>
            {
                Items = audioRecordResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
            return Ok(response);
        }

        private LikeResponseDto MapToResponse(Like like)
        {
            return new LikeResponseDto
            {
                Id = like.Id,
                User = new UserResponse
                {
                    Id = like.User.Id,
                    UserName = like.User.Username
                },
                LikedAt = like.LikedAt
            };
        }
        private AudioRecordWithLikeCountResponse MapToResponseWithLike(AudioRecord audioRecord)
        {
            return new AudioRecordWithLikeCountResponse
            {
                Id = audioRecord.Id,
                Title = audioRecord.Title,
                Artist = audioRecord.Artist,
                FilePath = audioRecord.FilePath,
                UploadDate = audioRecord.UploadDate,
                LikesCount = _context.Likes.Count(l => l.AudioRecordId == audioRecord.Id),
                Uploader = new UserResponse
                {
                    Id = audioRecord.Uploader.Id,
                    UserName = audioRecord.Uploader.Username
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
