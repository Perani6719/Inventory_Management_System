using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // 1. Added ILogger namespace
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesHistoryController : ControllerBase
    {
        private readonly ISalesHistory _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<SalesHistoryController> _logger; // 2. Declare Logger

        public SalesHistoryController(
            ISalesHistory repository,
            IMapper mapper,
            ShelfSenseDbContext context,
            ILogger<SalesHistoryController> logger) // 3. Inject Logger
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _logger = logger;
        }

        // 🔓 Accessible to manager and staff
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Staff or Manager attempting to retrieve all sales history records.");
            try
            {
                var sales = _repository.GetAll().ToList();
                var response = _mapper.Map<List<SalesHistoryResponse>>(sales);
                _logger.LogInformation("Successfully retrieved {Count} sales history records.", sales.Count);
                return Ok(new { message = "Sales history retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sales history.");
                return StatusCode(500, new { message = "Error retrieving sales history.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogDebug("Attempting to retrieve sales record ID: {SaleId}", id);
            try
            {
                var sale = await _repository.GetByIdAsync(id);
                if (sale == null)
                {
                    _logger.LogWarning("Sales record ID {SaleId} not found.", id);
                    return NotFound(new { message = $"Sale ID {id} not found." });
                }

                var response = _mapper.Map<SalesHistoryResponse>(sale);
                _logger.LogInformation("Sales record ID {SaleId} retrieved successfully.", id);
                return Ok(new { message = "Sales record retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales record ID {SaleId}.", id);
                return StatusCode(500, new { message = $"Error retrieving sale {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesHistoryCreateRequest request)
        {
            _logger.LogInformation("Manager attempting to create a new sales record for Product {ProductId} at Store {StoreId}.",
                request.ProductId, request.StoreId);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Sales record creation failed due to validation errors.");
                    return BadRequest(new
                    {
                        message = "Validation failed.",
                        errors = ModelState
                            .Where(e => e.Value.Errors.Count > 0)
                            .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
                            .ToList()
                    });
                }

                if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                {
                    _logger.LogWarning("Sales record creation failed: Store ID '{StoreId}' does not exist.", request.StoreId);
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                {
                    _logger.LogWarning("Sales record creation failed: Product ID '{ProductId}' does not exist.", request.ProductId);
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });
                }

                var entity = _mapper.Map<SalesHistory>(request);
                await _repository.AddAsync(entity);

                var response = _mapper.Map<SalesHistoryResponse>(entity);
                _logger.LogInformation("Sales record {SaleId} created successfully.", response.SaleId);
                return CreatedAtAction(nameof(GetById), new { id = response.SaleId }, new
                {
                    message = "Sales record created successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales record for Product {ProductId} at Store {StoreId}.",
                    request.ProductId, request.StoreId);
                return StatusCode(500, new { message = "Error creating sales record.", details = ex.Message });
            }
        }

        // 🔐 Manager-only
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] SalesHistoryCreateRequest request)
        {
            _logger.LogInformation("Manager attempting to update sales record ID {SaleId}.", id);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Sales record update failed for ID {SaleId} due to validation errors.", id);
                    return BadRequest(new
                    {
                        message = "Validation failed.",
                        errors = ModelState
                            .Where(e => e.Value.Errors.Count > 0)
                            .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
                            .ToList()
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Sales record update failed: Sale ID {SaleId} not found.", id);
                    return NotFound(new { message = $"Sale ID {id} not found." });
                }

                if (!await _context.Stores.AnyAsync(s => s.StoreId == request.StoreId))
                {
                    _logger.LogWarning("Sales record update failed for {SaleId}: Store ID '{StoreId}' does not exist.", id, request.StoreId);
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
                {
                    _logger.LogWarning("Sales record update failed for {SaleId}: Product ID '{ProductId}' does not exist.", id, request.ProductId);
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });
                }

                _mapper.Map(request, existing);
                await _repository.UpdateAsync(existing);

                _logger.LogInformation("Sales record ID {SaleId} updated successfully.", id);
                return Ok(new { message = $"Sales record ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sales record ID {SaleId}.", id);
                return StatusCode(500, new { message = $"Error updating sales record {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only with confirmation
        [Authorize(Roles = "admin,manager,staff")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm = false)
        {
            _logger.LogWarning("Manager attempting to delete sales record ID {SaleId}. Confirmation: {Confirmed}", id, confirm);
            try
            {
                if (!confirm)
                {
                    _logger.LogWarning("Sales record deletion for ID {SaleId} blocked: Deletion not confirmed.", id);
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Sales record deletion failed: Sale ID {SaleId} not found.", id);
                    return NotFound(new { message = $"Sale ID {id} not found." });
                }

                await _repository.DeleteAsync(id);
                _logger.LogInformation("Sales record ID {SaleId} deleted successfully.", id);
                return Ok(new { message = $"Sales record ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sales record ID {SaleId}.", id);
                return StatusCode(500, new { message = $"Error deleting sales record {id}.", details = ex.Message });
            }
        }
    }
}