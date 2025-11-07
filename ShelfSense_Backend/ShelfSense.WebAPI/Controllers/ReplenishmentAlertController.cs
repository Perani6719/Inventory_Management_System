using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Required for ILogger
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System.Net;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReplenishmentAlertController : ControllerBase
    {
        private readonly IReplenishmentAlert _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<ReplenishmentAlertController> _logger; // 1. Declare Logger

        public ReplenishmentAlertController(
            IReplenishmentAlert repository,
            IMapper mapper,
            ShelfSenseDbContext context,
            ILogger<ReplenishmentAlertController> logger) // 2. Accept ILogger in constructor
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _logger = logger;
        }

        // -----------------------------------------------------------------------------------------------------------------

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAlerts()
        {
            _logger.LogInformation("Retrieving all replenishment alerts.");
            try
            {
                var alerts = await _context.ReplenishmentAlerts
                    .Include(a => a.Shelf)
                    .Select(a => new
                    {
                        a.AlertId,
                        a.ProductId,
                        a.ShelfId,
                        a.PredictedDepletionDate,
                        a.UrgencyLevel,
                        a.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} alerts.", alerts.Count);

                return Ok(new
                {
                    message = "Replenishment alerts retrieved successfully.",
                    data = alerts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all alerts.");
                return StatusCode(500, new { message = "Error retrieving all alerts.", details = ex.Message });
            }
        }

        // -----------------------------------------------------------------------------------------------------------------

        //// 🔓 Get alert by ID
        //[Authorize(Roles = "admin,manager,staff")]
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetById(long id)
        //{
        //    _logger.LogInformation("Attempting to retrieve alert with ID: {AlertId}", id);
        //    try
        //    {
        //        var alert = await _repository.GetByIdAsync(id);
        //        if (alert == null)
        //        {
        //            _logger.LogWarning("Alert ID {AlertId} not found.", id);
        //            return NotFound(new { message = $"Alert ID {id} not found." });
        //        }

        //        var response = _mapper.Map<ReplenishmentAlertResponse>(alert);
        //        _logger.LogInformation("Alert ID {AlertId} retrieved successfully.", id);
        //        return Ok(new { message = "Replenishment alert retrieved successfully.", data = response });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving alert {AlertId}.", id);
        //        return StatusCode(500, new { message = $"Error retrieving alert {id}.", details = ex.Message });
        //    }
        //}

        // -----------------------------------------------------------------------------------------------------------------

        // 🔐 Create alert manually
        //[Authorize(Roles = "manager")]
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] ReplenishmentAlertResponse request)
        //{
        //    _logger.LogInformation("Manager is attempting to manually create a new replenishment alert for Product {ProductId} on Shelf {ShelfId}.", request.ProductId, request.ShelfId);
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            _logger.LogWarning("Manual alert creation failed due to model validation errors.");
        //            return BadRequest(new
        //            {
        //                message = "Validation failed.",
        //                errors = ModelState
        //                    .Where(e => e.Value!.Errors.Count > 0)
        //                    .SelectMany(kvp => kvp.Value!.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}"))
        //                    .ToList()
        //            });
        //        }

        //        if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
        //            return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

        //        if (!await _context.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId))
        //            return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

        //        var entity = _mapper.Map<ReplenishmentAlert>(request);
        //        entity.CreatedAt = DateTime.UtcNow;

        //        // Set default/derived properties
        //        if (string.IsNullOrEmpty(entity.UrgencyLevel))
        //        {
        //            entity.UrgencyLevel = "medium";
        //        }
        //        if (entity.PredictedDepletionDate == default)
        //        {
        //            entity.PredictedDepletionDate = DateTime.Today.AddDays(7); // Default to one week out if not set
        //        }


        //        await _repository.AddAsync(entity);
        //        _logger.LogInformation("Manually created alert with ID {AlertId} for Product {ProductId} on Shelf {ShelfId}.", entity.AlertId, entity.ProductId, entity.ShelfId);


        //        // 🧠 Log to InventoryReport (Logic remains commented out as entity types are dynamic)
        //        var shelfState = await _context.ProductShelves
        //            .FirstOrDefaultAsync(ps => ps.ProductId == entity.ProductId && ps.ShelfId == entity.ShelfId);

        //        if (shelfState != null)
        //        {
        //            bool reportExists = await _context.InventoryReports.AnyAsync(r =>
        //                r.ProductId == entity.ProductId &&
        //                r.ShelfId == entity.ShelfId &&
        //                r.ReportDate.Date == DateTime.Today.Date);

        //            if (!reportExists)
        //            {
        //                // Logic to create InventoryReport...
        //                // _context.InventoryReports.Add(report);
        //                // await _context.SaveChangesAsync();
        //                // _logger.LogDebug("Created new InventoryReport record for manual alert.");
        //            }
        //        }

        //        var response = _mapper.Map<ReplenishmentAlertResponse>(entity);
        //        return CreatedAtAction(nameof(GetById), new { id = response.AlertId }, new
        //        {
        //            message = "Replenishment alert created successfully.",
        //            data = response
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating manual replenishment alert.");
        //        return StatusCode(500, new { message = "Error creating replenishment alert.", details = ex.Message });
        //    }
        //}

        //// -----------------------------------------------------------------------------------------------------------------

        //// 🔐 Delete alert with confirmation
        //[Authorize(Roles = "manager")]
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        //{
        //    _logger.LogWarning("Manager attempting to delete alert ID {AlertId}. Confirmation: {Confirmation}", id, confirm);
        //    try
        //    {
        //        if (!confirm)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
        //            });
        //        }

        //        var existing = await _repository.GetByIdAsync(id);
        //        if (existing == null)
        //        {
        //            _logger.LogWarning("Delete request failed: Alert ID {AlertId} not found.", id);
        //            return NotFound(new { message = $"Alert ID {id} not found." });
        //        }

        //        try
        //        {
        //            await _repository.DeleteAsync(id);
        //            _logger.LogInformation("Successfully deleted alert ID {AlertId}.", id);
        //        }
        //        catch (DbUpdateException ex) when (ex.InnerException?.Message!.Contains("REFERENCE constraint") == true)
        //        {
        //            _logger.LogWarning(ex, "Deletion failed for Alert ID {AlertId} due to foreign key constraint.", id);
        //            return Conflict(new
        //            {
        //                message = $"Cannot delete Alert ID {id} because it is referenced in other records (e.g., RestockTask)."
        //            });
        //        }

        //        return Ok(new { message = $"Replenishment alert ID {id} deleted successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error while deleting Alert ID {AlertId}.", id);
        //        return StatusCode(500, new
        //        {
        //            message = $"Unexpected error while deleting Alert ID {id}.",
        //            details = ex.Message
        //        });
        //    }
        //}

        //// -----------------------------------------------------------------------------------------------------------------

        //// 🧠 AUTOMATED FULL REPLENISHMENT WORKFLOW 🧠
        //[HttpPost("trigger-all")]
        //public async Task<IActionResult> TriggerFullReplenishmentFlow()
        //{
        //    _logger.LogInformation("Starting full automated replenishment flow.");
        //    int alertsCreated = 0;
        //    int requestsCreated = 0;
        //    int tasksAssigned = 0;
        //    var errors = new List<object>();

        //    try
        //    {
        //        // ... (Velocity calculation logic remains the same)
        //        var velocityData = await (
        //            from sale in _context.SalesHistories
        //            group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
        //            select new
        //            {
        //                daily.Key.ProductId,
        //                DailySales = daily.Sum(x => x.Quantity)
        //            }
        //        ).ToListAsync();

        //        var salesVelocity = velocityData
        //            .GroupBy(x => x.ProductId)
        //            .ToDictionary(
        //                x => x.Key,
        //                g => g.Average(x => x.DailySales)
        //            );

        //        var shelves = await _context.ProductShelves
        //            .Include(ps => ps.Shelf)
        //            .ToListAsync();

        //        var fulfilledAlerts = new List<long>();

        //        foreach (var ps in shelves)
        //        {
        //            try
        //            {
        //                if (!salesVelocity.ContainsKey(ps.ProductId) || salesVelocity[ps.ProductId] < 0.1)
        //                {
        //                    _logger.LogDebug("Skipping Product {ProductId} on Shelf {ShelfId}: Low/Zero sales velocity.", ps.ProductId, ps.ShelfId);
        //                    continue;
        //                }

        //                var velocity = salesVelocity[ps.ProductId];
        //                var daysToDepletion = Math.Round((double)ps.Quantity / velocity, 2);
        //                var storeId = ps.Shelf.StoreId;
        //                var quantityNeeded = ps.Shelf.Capacity - ps.Quantity;

        //                if (daysToDepletion >= 3)
        //                {
        //                    _logger.LogDebug("Skipping Product {ProductId} on Shelf {ShelfId}: Days to depletion ({Days}) is >= 3.", ps.ProductId, ps.ShelfId, daysToDepletion);
        //                    continue;
        //                }

        //                var urgency = daysToDepletion switch
        //                {
        //                    <= 1 => "critical",
        //                    <= 2 => "high",
        //                    < 3 => "medium",
        //                    _ => "low"
        //                };

        //                var alert = await _context.ReplenishmentAlerts
        //                    .FirstOrDefaultAsync(a =>
        //                        a.ProductId == ps.ProductId &&
        //                        a.ShelfId == ps.ShelfId &&
        //                        a.FulfillmentNote == null);

        //                if (alert == null)
        //                {
        //                    alert = new ReplenishmentAlert
        //                    {
        //                        ProductId = ps.ProductId,
        //                        ShelfId = ps.ShelfId,
        //                        PredictedDepletionDate = DateTime.Today.AddDays(Math.Round(daysToDepletion)),
        //                        UrgencyLevel = urgency,
        //                        CreatedAt = DateTime.UtcNow
        //                    };

        //                    _context.ReplenishmentAlerts.Add(alert);
        //                    await _context.SaveChangesAsync();
        //                    alertsCreated++;
        //                    _logger.LogInformation("New {Urgency} alert created for Product {ProductId} on Shelf {ShelfId}. AlertId: {AlertId}", urgency, ps.ProductId, ps.ShelfId, alert.AlertId);
        //                }

        //                if ((urgency == "critical" || urgency == "high") && quantityNeeded > 0)
        //                {
        //                    // ... (StockRequest check and creation logic remains commented out)
        //                    bool requestExists = await _context.StockRequests.AnyAsync(r =>
        //                        r.ProductId == ps.ProductId &&
        //                        r.StoreId == storeId &&
        //                        r.DeliveryStatus == "requested");

        //                    if (!requestExists)
        //                    {
        //                        // _context.StockRequests.Add(request);
        //                        requestsCreated++;
        //                        // ...

        //                        if (string.IsNullOrEmpty(alert.FulfillmentNote))
        //                        {
        //                            alert.FulfillmentNote = $"Auto-requested via trigger-all on {DateTime.UtcNow:yyyy-MM-dd HH:mm}.";
        //                            _context.ReplenishmentAlerts.Update(alert);
        //                            fulfilledAlerts.Add(alert.AlertId);
        //                        }

        //                        // await _context.SaveChangesAsync();
        //                        _logger.LogInformation("Stock request simulated for Alert ID {AlertId} (Quantity: {QtyNeeded}).", alert.AlertId, quantityNeeded);
        //                    }
        //                }

        //                // ... (RestockTask check and assignment logic remains commented out)
        //                bool taskExists = await _context.RestockTasks.AnyAsync(t =>
        //                    t.ProductId == ps.ProductId &&
        //                    t.ShelfId == ps.ShelfId &&
        //                    t.Status == "pending");

        //                if (!taskExists && alert.AlertId > 0 &&
        //                    (requestsCreated > 0 || urgency == "critical" || urgency == "high"))
        //                {
        //                    var staff = await _context.Staffs
        //                        .Where(s => s.StoreId == storeId)
        //                        .FirstOrDefaultAsync();

        //                    if (staff != null)
        //                    {
        //                        // _context.RestockTasks.Add(task);
        //                        // await _context.SaveChangesAsync();
        //                        tasksAssigned++;
        //                        _logger.LogInformation("Restock task simulated assignment for Alert ID {AlertId} to Staff ID {StaffId}.", alert.AlertId, staff.StaffId);
        //                    }
        //                    else
        //                    {
        //                        _logger.LogWarning("Cannot assign task for Alert ID {AlertId}: No staff found in Store {StoreId}.", alert.AlertId, storeId);
        //                    }
        //                }
        //            }
        //            catch (Exception innerEx)
        //            {
        //                var errorItem = new
        //                {
        //                    ProductId = ps.ProductId,
        //                    ShelfId = ps.ShelfId,
        //                    Message = $"Inner processing error: {innerEx.Message}",
        //                    Details = innerEx.InnerException?.Message
        //                };
        //                errors.Add(errorItem);
        //                _logger.LogError(innerEx, "Error processing replenishment for Product {ProductId} on Shelf {ShelfId}.", ps.ProductId, ps.ShelfId);
        //            }
        //        }

        //        _logger.LogInformation("Full replenishment flow finished. Alerts created: {Alerts}, Requests created: {Requests}, Tasks assigned: {Tasks}", alertsCreated, requestsCreated, tasksAssigned);

        //        return Ok(new
        //        {
        //            message = "Full replenishment flow triggered. (Note: Entity updates are commented out due to missing concrete entity classes.)",
        //            alertsFound = alertsCreated + fulfilledAlerts.Count,
        //            requestsCreated,
        //            tasksAssigned,
        //            errorsReported = errors.Count,
        //            errors
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogCritical(ex, "Critical error during full replenishment flow setup.");
        //        return StatusCode(500, new
        //        {
        //            message = "Critical error during full replenishment flow setup.",
        //            details = ex.Message,
        //            errorsReported = errors.Count,
        //            errors
        //        });
        //    }
        //}

        //// -----------------------------------------------------------------------------------------------------------------

        //[HttpPost("send-request/{alertId}")]
        //public async Task<IActionResult> SendRequestToWarehouse(long alertId)
        //{
        //    _logger.LogInformation("Attempting to send warehouse request for Alert ID {AlertId}.", alertId);
        //    try
        //    {
        //        var alert = await _context.ReplenishmentAlerts
        //            .Include(a => a.Shelf)
        //            .FirstOrDefaultAsync(a => a.AlertId == alertId);

        //        if (alert == null)
        //        {
        //            _logger.LogWarning("Warehouse request failed: Alert ID {AlertId} not found.", alertId);
        //            return NotFound(new { message = "Alert not found." });
        //        }

        //        if (!Enum.TryParse<UrgencyLevel>(alert.UrgencyLevel, true, out var urgency))
        //            return BadRequest(new { message = "Invalid urgency level." });

        //        var productShelf = await _context.ProductShelves
        //            .Include(ps => ps.Shelf)
        //            .FirstOrDefaultAsync(ps =>
        //                ps.ProductId == alert.ProductId &&
        //                ps.ShelfId == alert.ShelfId);

        //        if (productShelf == null)
        //            return NotFound(new { message = "ProductShelf entry not found for this alert." });

        //        var quantityNeeded = productShelf.Shelf.Capacity - productShelf.Quantity;

        //        // Assuming StockRequest is an existing entity (Logic remains commented out)
        //        dynamic request = new
        //        {
        //            ProductId = alert.ProductId,
        //            StoreId = productShelf.Shelf.StoreId,
        //            Quantity = quantityNeeded,
        //            RequestDate = DateTime.UtcNow,
        //            DeliveryStatus = "requested",
        //            AlertId = alert.AlertId
        //        };

        //        // _context.StockRequests.Add(request);

        //        // ✅ Mark the alert as fulfilled
        //        alert.FulfillmentNote = $"Stock request sent to warehouse on {DateTime.UtcNow:yyyy-MM-dd HH:mm}.";
        //        _context.ReplenishmentAlerts.Update(alert);
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation("Stock request sent and Alert ID {AlertId} fulfilled. Qty: {Qty}, Store: {Store}", alertId, quantityNeeded, productShelf.Shelf.StoreId);

        //        return Ok(new
        //        {
        //            message = "Stock request sent to warehouse.",
        //            alertId = alert.AlertId,
        //            urgency = alert.UrgencyLevel,
        //            quantityRequested = quantityNeeded
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing warehouse request for Alert ID {AlertId}.", alertId);
        //        return StatusCode(500, new { message = "Error sending stock request.", details = ex.Message });
        //    }
        //}

        //// -----------------------------------------------------------------------------------------------------------------

        //// 🔐 Assign a RestockTask to staff for an existing alert
        //[Authorize(Roles = "manager,staff")]
        //[HttpPost("assign-task/{alertId}")]
        //public async Task<IActionResult> AssignRestockTask(long alertId, [FromQuery] long? staffId)
        //{
        //    _logger.LogInformation("Attempting to assign restock task for Alert ID {AlertId} to Staff ID {StaffId}.", alertId, staffId);
        //    try
        //    {
        //        var alert = await _context.ReplenishmentAlerts
        //            .Include(a => a.Shelf)
        //            .FirstOrDefaultAsync(a => a.AlertId == alertId);

        //        if (alert == null)
        //        {
        //            _logger.LogWarning("Task assignment failed: Alert ID {AlertId} not found.", alertId);
        //            return NotFound(new { message = $"Alert ID {alertId} not found." });
        //        }

        //        if (alert.FulfillmentNote != null)
        //        {
        //            _logger.LogWarning("Task assignment failed: Alert ID {AlertId} is already fulfilled.", alertId);
        //            return BadRequest(new { message = $"Alert ID {alertId} is already fulfilled and cannot be assigned a new task." });
        //        }

        //        // Check if a pending task already exists for this alert/shelf
        //        bool taskExists = await _context.RestockTasks.AnyAsync(t =>
        //            t.AlertId == alertId && t.Status == "pending");

        //        if (taskExists)
        //        {
        //            _logger.LogWarning("Task assignment failed: Pending restock task already exists for Alert ID {AlertId}.", alertId);
        //            return Conflict(new { message = $"A pending restock task for Alert ID {alertId} already exists." });
        //        }

        //        long assignedStaffId;
        //        long storeId = alert.Shelf.StoreId;

        //        if (staffId.HasValue)
        //        {
        //            bool staffInStore = await _context.Staffs.AnyAsync(s => s.StaffId == staffId.Value && s.StoreId == storeId);
        //            if (!staffInStore)
        //            {
        //                _logger.LogWarning("Task assignment failed: Staff ID {StaffId} is not in Store {StoreId}.", staffId.Value, storeId);
        //                return BadRequest(new { message = $"Staff ID {staffId.Value} is invalid or not assigned to the store for this shelf." });
        //            }
        //            assignedStaffId = staffId.Value;
        //        }
        //        else
        //        {
        //            var staff = await _context.Staffs
        //                .Where(s => s.StoreId == storeId)
        //                .OrderBy(s => s.StaffId)
        //                .FirstOrDefaultAsync();

        //            if (staff == null)
        //            {
        //                _logger.LogError("Task assignment failed: No staff found in Store {StoreId} for auto-assignment.", storeId);
        //                return StatusCode(500, new { message = "No staff found in the store to assign the task." });
        //            }
        //            assignedStaffId = staff.StaffId;
        //        }

        //        // Assuming RestockTask is an existing entity (Logic remains commented out)
        //        dynamic task = new
        //        {
        //            AlertId = alert.AlertId,
        //            ProductId = alert.ProductId,
        //            ShelfId = alert.ShelfId,
        //            AssignedTo = assignedStaffId,
        //            Status = "pending",
        //            AssignedAt = DateTime.UtcNow,
        //            Notes = $"Manually assigned task for {alert.UrgencyLevel} urgency alert."
        //        };

        //        // _context.RestockTasks.Add(task);
        //        // await _context.SaveChangesAsync();

        //        _logger.LogInformation("Restock task assigned successfully for Alert ID {AlertId} to Staff ID {StaffId}.", alertId, assignedStaffId);

        //        return Ok(new
        //        {
        //            message = "Restock task successfully assigned.",
        //            alertId = alert.AlertId,
        //            assignedToStaffId = assignedStaffId,
        //            // taskId = task.TaskId 
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error during restock task assignment for Alert ID {AlertId}.", alertId);
        //        return StatusCode(500, new { message = "Error assigning restock task.", details = ex.Message });
        //    }
        //}
    }
}