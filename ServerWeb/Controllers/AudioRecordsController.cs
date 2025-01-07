using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Нужен для IFormFile
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ServerWeb.BLL.Models;
using System.ComponentModel.DataAnnotations;
using ServerWeb.DAL.Context;


namespace YourSocialMedia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AudioRecordsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const string UploadsFolder = "uploads";

        public AudioRecordsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;

            if (!Directory.Exists(Path.Combine(_environment.WebRootPath, UploadsFolder)))
            {
                Directory.CreateDirectory(Path.Combine(_environment.WebRootPath, UploadsFolder));
            }
        }

        // GET: api/AudioRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AudioRecord>>> GetAudioRecords(int page = 1, int pageSize = 10)
        {
            var audioRecords = await _context.AudioRecords
               .Include(a => a.Uploader) // Загружаем пользователя-загрузчика
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (audioRecords == null || audioRecords.Count == 0)
            {
                return NoContent(); // Возвращаем 204, если нет записей
            }
            return Ok(audioRecords);
        }

        // GET: api/AudioRecords/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AudioRecord>> GetAudioRecord(int id)
        {
            var audioRecord = await _context.AudioRecords
                .Include(a => a.Uploader) // Загружаем пользователя-загрузчика
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audioRecord == null)
            {
                return NotFound();
            }

            return Ok(audioRecord);
        }

        // POST: api/AudioRecords
        [HttpPost]
        public async Task<ActionResult<AudioRecord>> PostAudioRecord([FromForm] AudioRecordCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.File == null || request.File.Length == 0 || !request.File.ContentType.StartsWith("audio/"))
            {
                return BadRequest("Invalid audio file upload.");
            }

            var userId = GetCurrentUserId(); // Получаем ID текущего пользователя
            if (userId == null)
            {
                return Unauthorized();
            }
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Unauthorized(); // Пользователь не найден, что странно, если он авторизован
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.File.FileName);
            var filePath = Path.Combine(_environment.WebRootPath, UploadsFolder, fileName);


            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }


            var audioRecord = new AudioRecord
            {
                Title = request.Title,
                Artist = request.Artist,
                FilePath = Path.Combine(UploadsFolder, fileName), // Сохраняем путь относительно wwwroot
                UploadDate = DateTime.Now,
                UserId = user.Id,
            };

            _context.AudioRecords.Add(audioRecord);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAudioRecord), new { id = audioRecord.Id }, audioRecord);
        }

        // PUT: api/AudioRecords/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAudioRecord(int id, [FromBody] AudioRecordUpdateRequest updatedAudioRecord)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != updatedAudioRecord.Id)
            {
                return BadRequest("Id in request URL does not match the id in the body.");
            }

            var audioRecord = await _context.AudioRecords.FindAsync(id);

            if (audioRecord == null)
            {
                return NotFound();
            }
            var userId = GetCurrentUserId();

            if (userId == null || audioRecord.UserId != userId)
            {
                return Forbid(); // Запрет на обновление не своего контента
            }

            audioRecord.Title = updatedAudioRecord.Title;
            audioRecord.Artist = updatedAudioRecord.Artist;


            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/AudioRecords/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudioRecord(int id)
        {
            var audioRecord = await _context.AudioRecords.FindAsync(id);

            if (audioRecord == null)
            {
                return NotFound();
            }
            var userId = GetCurrentUserId();
            if (userId == null || audioRecord.UserId != userId)
            {
                return Forbid(); // Запрет на удаление не своего контента
            }

            _context.AudioRecords.Remove(audioRecord);
            await _context.SaveChangesAsync();
            return NoContent();
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

        // DTO для POST запроса (Multipart/Form)
        public class AudioRecordCreateRequest
        {
            [Required]
            public string Title { get; set; }
            [Required]
            public string Artist { get; set; }
            [Required]
            public IFormFile File { get; set; }
        }
        public class AudioRecordUpdateRequest
        {
            [Required]
            public int Id { get; set; }
            [Required]
            public string Title { get; set; }
            [Required]
            public string Artist { get; set; }
        }
    }
}