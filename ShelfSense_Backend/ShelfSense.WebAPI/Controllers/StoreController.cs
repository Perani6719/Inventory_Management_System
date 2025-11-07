using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Required for ILogger
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<StoreController> _logger; // 1. Declare Logger

        public StoreController(IStoreRepository repository, IMapper mapper, ILogger<StoreController> logger) // 2. Inject Logger
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // 🔓 Accessible to manager and staff
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Retrieving all stores.");
            try
            {
                var stores = _repository.GetAll().ToList();
                var response = _mapper.Map<List<StoreResponse>>(stores);
                _logger.LogInformation("Successfully retrieved {Count} stores.", stores.Count);
                return Ok(new { message = "Stores retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stores.");
                return StatusCode(500, new { message = "Error retrieving stores.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogDebug("Attempting to retrieve store ID: {StoreId}", id);
            try
            {
                var store = await _repository.GetByIdAsync(id);
                if (store == null)
                {
                    _logger.LogWarning("Store ID {StoreId} not found.", id);
                    return NotFound(new { message = $"Store with ID {id} not found." });
                }

                var response = _mapper.Map<StoreResponse>(store);
                _logger.LogInformation("Store ID {StoreId} retrieved successfully.", id);
                return Ok(new { message = "Store retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving store ID {StoreId}.", id);
                return StatusCode(500, new { message = $"Error retrieving store {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StoreCreateRequest request)
        {
            _logger.LogInformation("Manager attempting to create a new store with Name: {StoreName}.", request?.StoreName);

            if (request == null)
            {
                _logger.LogWarning("Store creation failed: Request body was null.");
                return BadRequest(new { message = "Request body cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Store creation failed due to validation errors for Store: {StoreName}.", request.StoreName);
                return BadRequest(ModelState);
            }

            var store = _mapper.Map<Store>(request);

            try
            {
                await _repository.AddAsync(store);
                var response = _mapper.Map<StoreResponse>(store);
                _logger.LogInformation("Store ID {StoreId} created successfully with Name: {StoreName}.", store.StoreId, store.StoreName);

                return CreatedAtAction(nameof(GetById), new { id = response.StoreId }, new
                {
                    message = "Store created successfully.",
                    data = response
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Stores_StoreName") == true)
            {
                _logger.LogWarning("Store creation failed: Store name '{StoreName}' already exists (Unique Constraint violation).", request.StoreName);
                return Conflict(new { message = $"Store name '{request.StoreName}' already exists." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store with Name: {StoreName}.", request.StoreName);
                return StatusCode(500, new { message = "Error creating store.", details = ex.Message });
            }
        }

        // 🔐 Manager-only
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] StoreCreateRequest request)
        {
            _logger.LogInformation("Manager attempting to update store ID {StoreId}.", id);

            if (request == null)
            {
                _logger.LogWarning("Store update failed for ID {StoreId}: Request body was null.", id);
                return BadRequest(new { message = "Request body cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Store update failed for ID {StoreId} due to validation errors.", id);
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Store update failed: Store ID {StoreId} not found.", id);
                    return NotFound(new { message = $"Store with ID {id} not found." });
                }

                existing.StoreName = request.StoreName;
                existing.Address = request.Address;
                existing.City = request.City;
                existing.State = request.State;
                existing.PostalCode = request.PostalCode;

                await _repository.UpdateAsync(existing);
                _logger.LogInformation("Store ID {StoreId} updated successfully to Name: {StoreName}.", id, request.StoreName);

                return Ok(new { message = $"Store ID {id} updated successfully." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Stores_StoreName") == true)
            {
                _logger.LogWarning("Store update failed for ID {StoreId}: Store name '{StoreName}' already exists (Unique Constraint violation).", id, request.StoreName);
                return Conflict(new { message = $"Store name '{request.StoreName}' already exists." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store ID {StoreId}.", id);
                return StatusCode(500, new { message = $"Error updating store {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only with confirmation
        [Authorize(Roles = "admin,manager,staff")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm = false)
        {
            _logger.LogWarning("Manager attempting to delete store ID {StoreId}. Confirmation: {Confirmed}", id, confirm);
            if (!confirm)
            {
                _logger.LogWarning("Store deletion for ID {StoreId} blocked: Deletion not confirmed.", id);
                return BadRequest(new
                {
                    message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                });
            }

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Store deletion failed: Store ID {StoreId} not found.", id);
                    return NotFound(new { message = $"Store with ID {id} not found." });
                }

                await _repository.DeleteAsync(id);
                _logger.LogInformation("Store ID {StoreId} deleted successfully.", id);

                return Ok(new { message = $"Store ID {id} deleted successfully." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
            {
                _logger.LogWarning("Store deletion failed for ID {StoreId} due to foreign key constraints.", id);
                return Conflict(new
                {
                    message = $"Cannot delete Store ID {id} because it is referenced in other records (e.g., Staff, Shelf, or SalesHistory)."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store ID {StoreId}.", id);
                return StatusCode(500, new { message = $"Error deleting store {id}.", details = ex.Message });
            }
        }
    }
}