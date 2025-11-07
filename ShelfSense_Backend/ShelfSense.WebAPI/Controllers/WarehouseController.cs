using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data;

[Authorize(Roles = "warehouse")]
[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly ShelfSenseDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDeliveryStatusLog _statusLogRepository;
    private readonly IStockRequest _stockRequestRepository;

    public WarehouseController(
        ShelfSenseDbContext context,
        IMapper mapper,
        IDeliveryStatusLog statusLogRepository,
        IStockRequest stockRequestRepository)
    {
        _context = context;
        _mapper = mapper;
        _statusLogRepository = statusLogRepository;
        _stockRequestRepository = stockRequestRepository;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetIncomingRequests()
    {
        try
        {
            var requests = await _context.StockRequests
                .Include(r => r.Product)
                .Include(r => r.Store)
                .Include(r => r.Alert)
                .Where(r => r.DeliveryStatus == "requested" || r.DeliveryStatus == "in_transit" && r.AlertId != null)
                .OrderBy(r => r.RequestDate)
                .ToListAsync();

            var response = requests.Select(r => new
            {
                r.RequestId,
                r.ProductId,
                ProductName = r.Product?.ProductName,
                r.StoreId,
                StoreName = r.Store?.StoreName,
                r.Quantity,
                r.RequestDate,
                r.DeliveryStatus,
                AlertId = r.AlertId,
                Urgency = r.Alert?.UrgencyLevel,
                PredictedDepletionDate = r.Alert?.PredictedDepletionDate
            });

            return Ok(new { message = "Incoming stock requests for warehouse retrieved.", data = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving warehouse requests.", details = ex.Message });
        }
    }

    [HttpPut("{id}/dispatch")]
    public async Task<IActionResult> MarkAsInTransit(long id, [FromBody] DateTime? estimatedArrival)
    {
        try
        {
            await _stockRequestRepository.UpdateDeliveryStatusAsync(id, "in_transit", estimatedArrival);

            return Ok(new
            {
                message = $"Stock request ID {id} marked as in transit.",
                requestId = id,
                estimatedArrival = estimatedArrival ?? DateTime.UtcNow.AddDays(2)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error dispatching stock request ID {id}.", details = ex.Message });
        }
    }

    [HttpPost("{id}/mark-delivered")]
    public async Task<IActionResult> MarkAsDelivered(long id)
    {
        try
        {
            if (_context.Database.IsRelational())
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                await _stockRequestRepository.UpdateDeliveryStatusAsync(id, "delivered");
                await transaction.CommitAsync();
            }
            else
            {
                await _stockRequestRepository.UpdateDeliveryStatusAsync(id, "delivered");
            }

            var request = await _context.StockRequests
                .Include(r => r.Product)
                .Include(r => r.Store)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            return Ok(new
            {
                message = "Stock request marked as delivered, archived, and alert resolved.",
                requestId = request?.RequestId,
                alertId = request?.AlertId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error marking stock request {id} as delivered.", details = ex.Message });
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelRequest(long id, [FromBody] string? reason)
    {
        try
        {
            await _stockRequestRepository.UpdateDeliveryStatusAsync(id, "cancelled");

            return Ok(new
            {
                message = $"Stock request ID {id} has been cancelled.",
                data = new
                {
                    requestId = id,
                    cancelReason = reason ?? "Not specified"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error cancelling stock request ID {id}.", details = ex.Message });
        }
    }

    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        try
        {
            var requests = await _context.StockRequests
                .Where(r => r.DeliveryStatus == "requested")
                .ToListAsync();

            var response = _mapper.Map<List<StockRequestResponse>>(requests);

            return Ok(new { message = "Pending stock requests retrieved successfully.", data = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving pending stock requests.", details = ex.Message });
        }
    }
}