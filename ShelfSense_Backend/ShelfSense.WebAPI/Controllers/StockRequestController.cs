using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockRequestController : ControllerBase
    {
        private readonly IStockRequest _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<StockRequestController> _logger;
        private readonly IDeliveryStatusLog _statusLogRepository; // ✅ Injected

        public StockRequestController(
            IStockRequest repository,
            IMapper mapper,
            ShelfSenseDbContext context,
            ILogger<StockRequestController> logger,
            IDeliveryStatusLog statusLogRepository) // ✅ Added
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _logger = logger;
            _statusLogRepository = statusLogRepository;
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Retrieving all stock requests.");
            try
            {
                var requests = await _repository.GetAll().ToListAsync();
                var response = _mapper.Map<List<StockRequestResponse>>(requests);
                _logger.LogInformation("Successfully retrieved {Count} stock requests.", requests.Count);
                return Ok(new { message = "Stock requests retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stock requests.");
                return StatusCode(500, new { message = "Error retrieving stock requests.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogDebug("Attempting to retrieve stock request ID: {RequestId}", id);
            try
            {
                var request = await _repository.GetByIdAsync(id);
                if (request == null)
                {
                    _logger.LogWarning("Stock request ID {RequestId} not found.", id);
                    return NotFound(new { message = $"Stock request ID {id} not found." });
                }

                var response = _mapper.Map<StockRequestResponse>(request);
                _logger.LogInformation("Stock request ID {RequestId} retrieved successfully.", id);
                return Ok(new { message = "Stock request retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock request ID {RequestId}.", id);
                return StatusCode(500, new { message = $"Error retrieving stock request {id}.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost("create-from-alerts")]
        public async Task<IActionResult> CreateRequestsFromAlerts()
        {
            _logger.LogInformation("Manager triggered automated stock request creation from open alerts.");
            try
            {
                var newRequests = await _repository.CreateRequestFromAlertsByUrgenyAsync();

                foreach (var request in newRequests)
                {
                    await LogStatusChange(request.RequestId, request.AlertId, "requested"); // ✅ Log each new request
                }

                if (newRequests.Count == 0)
                {
                    _logger.LogInformation("No open alerts found that required a new stock request.");
                    return Ok(new { message = "No open alerts found that require a new stock request." });
                }

                _logger.LogInformation("Successfully created {Count} stock requests from alerts.", newRequests.Count);
                return Ok(new
                {
                    message = $"{newRequests.Count} stock requests successfully created from open alerts.",
                    data = newRequests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock requests from alerts.");
                return StatusCode(500, new { message = "Error creating stock requests from alerts.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("delivered")]
        public async Task<IActionResult> GetDeliveredRequests()
        {
            _logger.LogInformation("Retrieving delivered stock requests.");
            try
            {
                var delivered = await _context.DeliveredStockRequests
                    .Include(d => d.Product)
                    .Include(d => d.Store)
                    .OrderByDescending(d => d.DeliveredAt)
                    .Select(d => new
                    {
                        d.OriginalRequestId,
                        ProductName = d.Product.ProductName,
                        StoreName = d.Store.StoreName,
                        d.Quantity,
                        d.DeliveredAt,
                        d.AlertId,
                        Source = d.AlertId != null ? "Alert-triggered" : "Manual/Direct"
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} delivered stock requests.", delivered.Count);
                return Ok(new { message = "Delivered stock requests retrieved.", data = delivered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivered requests.");
                return StatusCode(500, new { message = "Error retrieving delivered requests.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelledRequests()
        {
            _logger.LogInformation("Retrieving cancelled stock requests.");
            try
            {
                var cancelled = await _context.StockRequests
                    .Include(r => r.Product)
                    .Include(r => r.Store)
                    .Where(r => r.DeliveryStatus == "cancelled")
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        r.RequestId,
                        ProductName = r.Product.ProductName,
                        StoreName = r.Store.StoreName,
                        r.Quantity,
                        r.RequestDate,
                        r.EstimatedTimeOfArrival
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} cancelled stock requests.", cancelled.Count);
                return Ok(new { message = "Cancelled stock requests retrieved.", data = cancelled });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cancelled requests.");
                return StatusCode(500, new { message = "Error retrieving cancelled requests.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("in-transit")]
        public async Task<IActionResult> GetInTransitRequests()
        {
            _logger.LogInformation("Retrieving in-transit stock requests.");
            try
            {
                var inTransit = await _context.StockRequests
                    .Include(r => r.Product)
                    .Include(r => r.Store)
                    .Where(r => r.DeliveryStatus == "in_transit")
                    .OrderByDescending(r => r.EstimatedTimeOfArrival)
                    .Select(r => new
                    {
                        r.RequestId,
                        ProductName = r.Product.ProductName,
                        StoreName = r.Store.StoreName,
                        r.Quantity,
                        r.RequestDate,
                        r.EstimatedTimeOfArrival
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} in-transit stock requests.", inTransit.Count);
                return Ok(new { message = "In-transit stock requests retrieved.", data = inTransit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving in-transit requests.");
                return StatusCode(500, new { message = "Error retrieving in-transit requests.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            _logger.LogInformation("Retrieving pending/requested stock requests.");
            try
            {
                var pending = await _context.StockRequests
                    .Include(r => r.Product)
                    .Include(r => r.Store)
                    .Where(r => r.DeliveryStatus == "requested" || r.DeliveryStatus == "pending")
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        r.RequestId,
                        ProductName = r.Product.ProductName,
                        StoreName = r.Store.StoreName,
                        r.Quantity,
                        r.RequestDate,
                        r.EstimatedTimeOfArrival
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} pending stock requests.", pending.Count);
                return Ok(new { message = "Pending stock requests retrieved.", data = pending });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending requests.");
                return StatusCode(500, new { message = "Error retrieving pending requests.", details = ex.Message });
            }
        }

        private async Task LogStatusChange(long requestId, long? alertId, string status)
        {
            var logEntry = new DeliveryStatusLog
            {
                RequestId = requestId,
                AlertId = alertId,
                DeliveryStatus = status,
                StatusChangedAt = DateTime.UtcNow
            };

            await _statusLogRepository.AddAsync(logEntry);
        }
    }
}