using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // ADDED
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Services;
using static ShelfSense.Application.DTOs.CategoryDto;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryController> _logger; // ADDED: Private readonly field for ILogger
        private readonly BlobStorageService _blobService;
        public CategoryController(ICategoryRepository repository, IMapper mapper, ILogger<CategoryController> logger, BlobStorageService blobService) // ADDED: ILogger to the constructor
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger; // ADDED: Assignment in the constructor
            _blobService = blobService;
        }

        // 🔓 Accessible to any authenticated user
        [Authorize(Roles= "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Attempting to retrieve all categories."); // LOGGING START
            try
            {
                var categories = _repository.GetAll().ToList();
                var response = _mapper.Map<List<CategoryResponse>>(categories);
                _logger.LogInformation("Successfully retrieved {Count} categories.", response.Count); // LOGGING SUCCESS
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all categories."); // LOGGING ERROR
                return StatusCode(500, new { message = "An error occurred while retrieving categories.", detail = ex.Message });
            }
        }

        // 🔓 Accessible to any authenticated user
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("Attempting to retrieve category with ID: {CategoryId}.", id); // LOGGING START
            try
            {
                var category = await _repository.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found with ID: {CategoryId}.", id); // LOGGING NOT FOUND
                    return NotFound(new { message = $"Category with ID {id} not found." });
                }

                var response = _mapper.Map<CategoryResponse>(category);
                _logger.LogInformation("Successfully retrieved category: {CategoryName} (ID: {CategoryId}).", response.CategoryName, id); // LOGGING SUCCESS
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving category ID: {CategoryId}.", id); // LOGGING ERROR
                return StatusCode(500, new { message = $"An error occurred while retrieving category {id}.", detail = ex.Message });
            }
        }

        // 🔐 Restricted to manager role
        //[Authorize(Roles = "admin,manager,staff")] // ADDED ROLE CHECK
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CategoryCreateRequest request)
        //{
        //    _logger.LogInformation("Attempting to create a new category: {CategoryName} by user {User}", request?.CategoryName, User.Identity?.Name); // LOGGING START
        //    try
        //    {
        //        if (request == null)
        //        {
        //            _logger.LogWarning("Category creation failed: Request body was null."); // LOGGING BAD REQUEST
        //            return BadRequest(new { message = "Request body cannot be null." });
        //        }

        //        if (!ModelState.IsValid)
        //        {
        //            _logger.LogWarning("Category creation failed: Invalid model state for category {CategoryName}.", request.CategoryName); // LOGGING BAD REQUEST
        //            return BadRequest(ModelState);
        //        }

        //        var category = _mapper.Map<Category>(request);

        //        try
        //        {
        //            await _repository.AddAsync(category);
        //        }
        //        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
        //        {
        //            _logger.LogWarning(ex, "Category creation failed due to duplicate name: {CategoryName}", request.CategoryName); // LOGGING CONFLICT
        //            return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
        //        }

        //        var response = _mapper.Map<CategoryResponse>(category);
        //        _logger.LogInformation("Category created successfully: {CategoryName} (ID: {CategoryId})", response.CategoryName, response.CategoryId); // LOGGING SUCCESS

        //        return CreatedAtAction(nameof(GetById), new { id = response.CategoryId }, new
        //        {
        //            message = $"Category '{response.CategoryName}' created successfully.",
        //            data = response
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An unexpected error occurred while creating category: {CategoryName}", request?.CategoryName); // LOGGING ERROR
        //        return StatusCode(500, new { message = "An error occurred while creating the category.", detail = ex.Message });
        //    }
        //}

        //// 🔐 Restricted to manager role
        //[Authorize(Roles = "admin,manager,staff")]
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(long id, [FromBody] CategoryCreateRequest request)
        //{
        //    _logger.LogInformation("Attempting to update category ID: {CategoryId} to name: {NewName}", id, request?.CategoryName);

        //    try
        //    {
        //        if (request == null)
        //        {
        //            _logger.LogWarning("Category update failed for ID {CategoryId}: Request body was null.", id);
        //            return BadRequest(new { message = "Request body cannot be null." });
        //        }

        //        if (!ModelState.IsValid)
        //        {
        //            _logger.LogWarning("Category update failed for ID {CategoryId}: Invalid model state.", id);
        //            return BadRequest(ModelState);
        //        }

        //        var existing = await _repository.GetByIdAsync(id);
        //        if (existing == null)
        //        {
        //            _logger.LogWarning("Category update failed: Category ID {CategoryId} not found.", id);
        //            return NotFound(new { message = $"Category with ID {id} not found." });
        //        }

        //        string oldName = existing.CategoryName;

        //        // ✅ Update only name and description
        //        existing.CategoryName = request.CategoryName;
        //        existing.Description = request.Description;

        //        try
        //        {
        //            await _repository.UpdateAsync(existing);
        //            _logger.LogInformation("Category ID {CategoryId} updated successfully. Name: '{OldName}' → '{NewName}'.",
        //                id, oldName, existing.CategoryName);
        //        }
        //        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
        //        {
        //            _logger.LogWarning(ex, "Category update failed for ID {CategoryId} due to duplicate name: {CategoryName}", id, request.CategoryName);
        //            return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
        //        }

        //        return Ok(new { message = $"Category with ID {id} updated successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An unexpected error occurred while updating category ID: {CategoryId}.", id);
        //        return StatusCode(500, new { message = $"An error occurred while updating category {id}.", detail = ex.Message });
        //    }
        //}

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CategoryCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to create a new category: {CategoryName} by user {User}", request?.CategoryName, User.Identity?.Name);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Category creation failed: Request body was null.");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Category creation failed: Invalid model state for category {CategoryName}.", request.CategoryName);
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
                        _logger.LogError(ex, "Image upload failed for category: {CategoryName}", request.CategoryName);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                var category = _mapper.Map<Category>(request);
                category.ImageUrl = imageUrl;

                try
                {
                    await _repository.AddAsync(category);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
                {
                    _logger.LogWarning(ex, "Category creation failed due to duplicate name: {CategoryName}", request.CategoryName);
                    return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
                }

                var response = _mapper.Map<CategoryResponse>(category);
                _logger.LogInformation("Category created successfully: {CategoryName} (ID: {CategoryId})", response.CategoryName, response.CategoryId);

                return CreatedAtAction(nameof(GetById), new { id = response.CategoryId }, new
                {
                    message = $"Category '{response.CategoryName}' created successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating category: {CategoryName}", request?.CategoryName);
                return StatusCode(500, new { message = "An error occurred while creating the category.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromForm] CategoryCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to update category ID: {CategoryId} to name: {NewName}", id, request?.CategoryName);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Category update failed for ID {CategoryId}: Request body was null.", id);
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Category update failed for ID {CategoryId}: Invalid model state.", id);
                    return BadRequest(ModelState);
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Category update failed: Category ID {CategoryId} not found.", id);
                    return NotFound(new { message = $"Category with ID {id} not found." });
                }

                string oldName = existing.CategoryName;

                existing.CategoryName = request.CategoryName;
                existing.Description = request.Description;

                if (image != null)
                {
                    try
                    {
                        existing.ImageUrl = await _blobService.UploadImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Image upload failed during update for category ID: {CategoryId}", id);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                try
                {
                    await _repository.UpdateAsync(existing);
                    _logger.LogInformation("Category ID {CategoryId} updated successfully. Name: '{OldName}' → '{NewName}'.", id, oldName, existing.CategoryName);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Category_CategoryName") == true)
                {
                    _logger.LogWarning(ex, "Category update failed for ID {CategoryId} due to duplicate name: {CategoryName}", id, request.CategoryName);
                    return Conflict(new { message = $"Category name '{request.CategoryName}' already exists." });
                }

                return Ok(new { message = $"Category with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating category ID: {CategoryId}.", id);
                return StatusCode(500, new { message = $"An error occurred while updating category {id}.", detail = ex.Message });
            }
        }



        // 🔐 Restricted to manager role
        [Authorize(Roles = "admin,manager,staff")] // ADDED ROLE CHECK
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            _logger.LogInformation("Attempting to delete category ID: {CategoryId}. Confirmation: {Confirmation}", id, confirm); // LOGGING START
            try
            {
                if (!confirm)
                {
                    _logger.LogWarning("Category deletion for ID {CategoryId} rejected: Deletion not confirmed.", id); // LOGGING BAD REQUEST
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Category deletion failed: Category ID {CategoryId} not found.", id); // LOGGING NOT FOUND
                    return NotFound(new { message = $"Category with ID {id} not found." });
                }

                try
                {
                    await _repository.DeleteAsync(id);
                    _logger.LogInformation("Category ID {CategoryId} deleted successfully.", id); // LOGGING SUCCESS
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    _logger.LogWarning(ex, "Category deletion failed for ID {CategoryId} due to foreign key constraint violation.", id); // LOGGING CONFLICT
                    return Conflict(new
                    {
                        message = $"Cannot delete Category ID {id} because it is referenced in other records (e.g., Products or InventoryReports)."
                    });
                }

                return Ok(new { message = $"Category with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting category ID: {CategoryId}.", id); // LOGGING ERROR
                return StatusCode(500, new { message = $"An error occurred while deleting category {id}.", detail = ex.Message });
            }
        }
    }
}