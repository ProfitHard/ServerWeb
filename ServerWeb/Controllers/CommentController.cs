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

namespace ServerWeb.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        [Authorize] // Все методы требуют аутентификации
        public class CommentController : ControllerBase
        {
            private readonly AppDbContext _context;

            public CommentController(AppDbContext context)
            {
                _context = context;
            }

            // GET: api/Comment?audioRecordId=5&page=1&pageSize=10
            [HttpGet]
            public async Task<ActionResult<PagedListResponse<CommentResponse>>> GetComments(int audioRecordId, int page = 1, int pageSize = 10)
            {
                // 1. Проверяем, существует ли AudioRecord с данным ID
                if (!await _context.AudioRecords.AnyAsync(ar => ar.Id == audioRecordId))
                {
                    return NotFound($"AudioRecord with ID '{audioRecordId}' not found.");
                }

                // 2. Получаем общее количество комментариев для данной аудиозаписи
                var totalItems = await _context.Comments
                    .Where(c => c.AudioRecordId == audioRecordId)
                    .CountAsync();

                // 3. Вычисляем общее количество страниц
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // 4. Получаем комментарии с пагинацией и включаем данные автора
                var comments = await _context.Comments
                    .Where(c => c.AudioRecordId == audioRecordId)
                    .Include(c => c.Author)
                    .OrderByDescending(c => c.CreatedAt) // Сортировка по дате создания (новые сверху)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 5. Если комментариев нет, возвращаем NoContent (204)
                if (comments == null || comments.Count == 0)
                {
                    return NoContent();
                }

                // 6. Преобразуем сущности в DTO
                var commentResponses = comments.Select(MapToResponse).ToList();

                // 7. Создаем объект ответа с пагинацией
                var response = new PagedListResponse<CommentResponse>
                {
                    Items = commentResponses,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                };

                // 8. Возвращаем успешный ответ
                return Ok(response);
            }


            // GET: api/Comment/5
            [HttpGet("{id}")]
            public async Task<ActionResult<CommentResponse>> GetComment(int id)
            {
                // 1. Получаем комментарий по ID и включаем данные автора
                var comment = await _context.Comments
                    .Include(c => c.Author)
                    .FirstOrDefaultAsync(c => c.Id == id);

                // 2. Если комментарий не найден, возвращаем NotFound (404)
                if (comment == null)
                {
                    return NotFound($"Comment with ID '{id}' not found.");
                }

                // 3. Преобразуем сущность в DTO и возвращаем успешный ответ
                return Ok(MapToResponse(comment));
            }


            // POST: api/Comment
            [HttpPost]
            public async Task<ActionResult<CommentResponse>> PostComment([FromBody] CommentCreateRequest request)
            {
                // 1. Проверяем Model State
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                // 2. Проверяем, существует ли AudioRecord с данным ID
                if (!await _context.AudioRecords.AnyAsync(ar => ar.Id == request.AudioRecordId))
                {
                    return NotFound($"AudioRecord with ID '{request.AudioRecordId}' not found.");
                }
                // 3. Получаем ID пользователя из Claims
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User not authenticated.");
                }
                // 4. Находим пользователя в БД
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Unauthorized("User not found, try to re-authenticate.");
                }

                // 5. Создаем новый комментарий
                var comment = new Comment
                {
                    Content = request.Content,
                    CreatedAt = DateTime.Now,
                    UserId = user.Id,
                    AudioRecordId = request.AudioRecordId
                };

                // 6. Добавляем комментарий в БД
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // 7. Возвращаем успешный ответ (201 Created)
                return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, MapToResponse(comment));
            }


            // PUT: api/Comment/5
            [HttpPut("{id}")]
            public async Task<IActionResult> PutComment(int id, [FromBody] CommentUpdateRequest updatedComment)
            {
                // 1. Проверяем Model State
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                // 2. Проверяем, что ID в URL совпадает с ID в теле запроса
                if (id != updatedComment.Id)
                {
                    return BadRequest("Id in request URL does not match the id in the body.");
                }

                // 3. Находим комментарий
                var comment = await _context.Comments.FindAsync(id);

                // 4. Если комментарий не найден, возвращаем NotFound (404)
                if (comment == null)
                {
                    return NotFound($"Comment with ID '{id}' not found.");
                }

                // 5. Проверяем, является ли текущий пользователь автором комментария
                var userId = GetCurrentUserId();
                if (userId == null || comment.UserId != userId)
                {
                    return Forbid("You are not allowed to update this comment.");
                }

                // 6. Обновляем комментарий
                comment.Content = updatedComment.Content;

                // 7. Сохраняем изменения
                await _context.SaveChangesAsync();

                // 8. Возвращаем успешный ответ (204 No Content)
                return NoContent();
            }


            // DELETE: api/Comment/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteComment(int id)
            {
                // 1. Находим комментарий
                var comment = await _context.Comments.FindAsync(id);

                // 2. Если комментарий не найден, возвращаем NotFound (404)
                if (comment == null)
                {
                    return NotFound($"Comment with ID '{id}' not found.");
                }

                // 3. Проверяем, является ли текущий пользователь автором комментария
                var userId = GetCurrentUserId();
                if (userId == null || comment.UserId != userId)
                {
                    return Forbid("You are not allowed to delete this comment.");
                }
                // 4. Удаляем комментарий
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                // 5. Возвращаем успешный ответ (204 No Content)
                return NoContent();
            }


            // Метод для преобразования сущности Comment в CommentResponse
            private CommentResponse MapToResponse(Comment comment)
            {
                return new CommentResponse
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    Author = new UserResponse
                    {
                        Id = comment.Author.Id,
                        UserName = comment.Author.Username
                    }
                };
            }


            // Метод для получения ID текущего пользователя из Claims
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