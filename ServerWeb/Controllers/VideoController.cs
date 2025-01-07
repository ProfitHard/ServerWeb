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
    public class VideoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VideoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Video?page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedListResponse<VideoResponse>>> GetVideos(int page = 1, int pageSize = 10)
        {
            var totalItems = await _context.Videos.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var videos = await _context.Videos
                .Include(v => v.Uploader)
                .OrderByDescending(v => v.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            if (videos == null || videos.Count == 0)
            {
                return NoContent();
            }
            var videoResponses = videos.Select(v => MapToResponse(v)).ToList();
            var response = new PagedListResponse<VideoResponse>
            {
                Items = videoResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
            return Ok(response);
        }

        // GET: api/Video/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VideoResponse>> GetVideo(int id)
        {
            var video = await _context.Videos
                .Include(v => v.Uploader)
                 .FirstOrDefaultAsync(v => v.Id == id);

            if (video == null)
            {
                return NotFound($"Video with ID '{id}' not found.");
            }
            return Ok(MapToResponse(video));
        }

        // POST: api/Video
        [HttpPost]
        public async Task<ActionResult<VideoResponse>> PostVideo([FromBody] VideoCreateRequest request)
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
            var video = new Video
            {
                Title = request.Title,
                Description = request.Description,
                FilePath = request.FilePath,
                UploadDate = DateTime.Now,
                UserId = user.Id
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVideo), new { id = video.Id }, MapToResponse(video));
        }

        // PUT: api/Video/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVideo(int id, [FromBody] VideoUpdateRequest updatedVideo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (id != updatedVideo.Id)
            {
                return BadRequest("Id in request URL does not match the id in the body.");
            }
            var video = await _context.Videos.FindAsync(id);

            if (video == null)
            {
                return NotFound($"Video with ID '{id}' not found.");
            }
            var userId = GetCurrentUserId();
            if (userId == null || video.UserId != userId)
            {
                return Forbid("You are not allowed to update this video.");
            }

            video.Title = updatedVideo.Title;
            video.Description = updatedVideo.Description;
            video.FilePath = updatedVideo.FilePath;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Video/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var video = await _context.Videos.FindAsync(id);

            if (video == null)
            {
                return NotFound($"Video with ID '{id}' not found.");
            }
            var userId = GetCurrentUserId();
            if (userId == null || video.UserId != userId)
            {
                return Forbid("You are not allowed to delete this video.");
            }
            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private VideoResponse MapToResponse(Video video)
        {
            return new VideoResponse
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                FilePath = video.FilePath,
                UploadDate = video.UploadDate,
                Uploader = new UserResponse
                {
                    Id = video.Uploader.Id,
                    UserName = video.Uploader.Username
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
