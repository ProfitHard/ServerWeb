using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ServerWeb.BLL.DTO;
using ServerWeb.BLL.Models;
using ServerWeb.DAL.Context;


namespace ServerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Post?page=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedListResponse<PostWithCommentCountResponse>>> GetPosts(int page = 1, int pageSize = 10)
        {
            var totalItems = await _context.Posts.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var posts = await _context.Posts
                  .Include(p => p.Author)
                 .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            if (posts == null || posts.Count == 0)
            {
                return NoContent();
            }
            var postResponses = posts.Select(p => MapToResponseWithCommentCount(p)).ToList();
            var response = new PagedListResponse<PostWithCommentCountResponse>
            {
                Items = postResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return Ok(response);

        }

        // GET: api/Post/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PostResponse>> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound($"Post with ID '{id}' not found.");
            }

            return Ok(MapToResponse(post));
        }

        // POST: api/Post
        [HttpPost]
        public async Task<ActionResult<PostResponse>> PostPost([FromBody] PostCreateRequest request)
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
            var post = new Post
            {
                Content = request.Content,
                CreatedAt = DateTime.Now,
                UserId = user.Id,
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, MapToResponse(post));
        }

        // PUT: api/Post/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPost(int id, [FromBody] PostUpdateRequest updatedPost)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (id != updatedPost.Id)
            {
                return BadRequest("Id in request URL does not match the id in the body.");
            }
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound($"Post with ID '{id}' not found.");
            }

            var userId = GetCurrentUserId();
            if (userId == null || post.UserId != userId)
            {
                return Forbid("You are not allowed to update this post.");
            }

            post.Content = updatedPost.Content;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Post/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound($"Post with ID '{id}' not found.");
            }
            var userId = GetCurrentUserId();
            if (userId == null || post.UserId != userId)
            {
                return Forbid("You are not allowed to delete this post.");
            }
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private PostResponse MapToResponse(Post post)
        {
            return new PostResponse
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                Author = new UserResponse
                {
                    Id = post.Author.Id,
                    UserName = post.Author.Username
                }
            };
        }
        private PostWithCommentCountResponse MapToResponseWithCommentCount(Post post)
        {
            return new PostWithCommentCountResponse
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                CommentsCount = _context.Comments.Count(c => c.UserId == post.UserId),
                Author = new UserResponse
                {
                    Id = post.Author.Id,
                    UserName = post.Author.Username
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
