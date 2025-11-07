using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Services;
//using static ShelfSense.Application.DTOs.ShelfDto;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelfController : ControllerBase
    {
        private readonly IShelfRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ShelfController> _logger;
        private readonly BlobStorageService _blobService;

        public ShelfController(
            IShelfRepository repository,
            IMapper mapper,
            ILogger<ShelfController> logger,
            BlobStorageService blobService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Attempting to retrieve all shelves.");
            try
            {
                var shelves = _repository.GetAll().ToList();
                var response = _mapper.Map<List<ShelfResponse>>(shelves);
                _logger.LogInformation("Successfully retrieved {Count} shelves.", response.Count);
                return Ok(new { message = "Shelves retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving shelves.");
                return StatusCode(500, new { message = "Error retrieving shelves.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("Attempting to retrieve shelf ID: {ShelfId}", id);
            try
            {
                var shelf = await _repository.GetByIdAsync(id);
                if (shelf == null)
                {
                    _logger.LogWarning("Shelf not found with ID: {ShelfId}", id);
                    return NotFound(new { message = $"Shelf with ID {id} not found." });
                }

                var response = _mapper.Map<ShelfResponse>(shelf);
                _logger.LogInformation("Shelf ID {ShelfId} retrieved successfully.", id);
                return Ok(new { message = "Shelf retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving shelf ID: {ShelfId}", id);
                return StatusCode(500, new { message = $"Error retrieving shelf {id}.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ShelfCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to create shelf with Code: {ShelfCode} for Store: {StoreId}", request?.ShelfCode, request?.StoreId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Shelf creation failed: Request body was null.");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Shelf creation failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                string? imageUrl = null;
                if (image != null)
                {
                    try
                    {
                        imageUrl = await _blobService.UploadImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Image upload failed for shelf: {ShelfCode}", request.ShelfCode);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                var shelf = _mapper.Map<Shelf>(request);
                shelf.ImageUrl = imageUrl;

                try
                {
                    await _repository.AddAsync(shelf);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Shelves_ShelfCode") == true)
                {
                    _logger.LogWarning(ex, "Shelf creation failed due to duplicate code: {ShelfCode}", request.ShelfCode);
                    return Conflict(new { message = $"Shelf code '{request.ShelfCode}' already exists." });
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Shelves_Stores_StoreId") == true)
                {
                    _logger.LogWarning(ex, "Shelf creation failed due to invalid Store ID: {StoreId}", request.StoreId);
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                var response = _mapper.Map<ShelfResponse>(shelf);
                _logger.LogInformation("Shelf created successfully: ID {ShelfId}, Code {ShelfCode}", response.ShelfId, response.ShelfCode);

                return CreatedAtAction(nameof(GetById), new { id = response.ShelfId }, new
                {
                    message = "Shelf created successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating shelf with Code: {ShelfCode}", request?.ShelfCode);
                return StatusCode(500, new { message = "Error creating shelf.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromForm] ShelfCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to update shelf ID: {ShelfId}", id);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Shelf update failed: Request body was null.");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Shelf update failed: Invalid model state.");
                    return BadRequest(ModelState);
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Shelf update failed: Shelf ID {ShelfId} not found.", id);
                    return NotFound(new { message = $"Shelf with ID {id} not found." });
                }

                existing.ShelfCode = request.ShelfCode;
                existing.StoreId = request.StoreId;
                existing.LocationDescription = request.LocationDescription;

                if (image != null)
                {
                    try
                    {
                        existing.ImageUrl = await _blobService.UploadImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Image upload failed during update for shelf ID: {ShelfId}", id);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                try
                {
                    await _repository.UpdateAsync(existing);
                    _logger.LogInformation("Shelf ID {ShelfId} updated successfully to Code: {ShelfCode}", id, request.ShelfCode);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Shelves_ShelfCode") == true)
                {
                    _logger.LogWarning(ex, "Shelf update failed due to duplicate code: {ShelfCode}", request.ShelfCode);
                    return Conflict(new { message = $"Shelf code '{request.ShelfCode}' already exists." });
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_Shelves_Stores_StoreId") == true)
                {
                    _logger.LogWarning(ex, "Shelf update failed due to invalid Store ID: {StoreId}", request.StoreId);
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                return Ok(new { message = $"Shelf ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating shelf ID: {ShelfId}", id);
                return StatusCode(500, new { message = $"Error updating shelf {id}.", detail = ex.Message });
            }
        }


        [Authorize(Roles = "admin,manager,staff")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            _logger.LogInformation("Attempting to delete shelf ID: {ShelfId}. Confirmation: {Confirmed}", id, confirm);

            try
            {
                if (!confirm)
                {
                    _logger.LogWarning("Shelf deletion blocked: Confirmation header missing for ID {ShelfId}", id);
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Shelf deletion failed: Shelf ID {ShelfId} not found.", id);
                    return NotFound(new { message = $"Shelf with ID {id} not found." });
                }

                try
                {
                    await _repository.DeleteAsync(id);
                    _logger.LogInformation("Shelf ID {ShelfId} deleted successfully.", id);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    _logger.LogWarning(ex, "Shelf deletion failed for ID {ShelfId} due to foreign key constraint violation.", id);
                    return Conflict(new
                    {
                        message = $"Cannot delete Shelf ID {id} because it is referenced in other records (e.g., ProductShelf or ReplenishmentAlert)."
                    });
                }

                return Ok(new { message = $"Shelf ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting shelf ID: {ShelfId}.", id);
                return StatusCode(500, new { message = $"An error occurred while deleting shelf {id}.", detail = ex.Message });
            }
        }

    }
}