using ShelfSense.Application.DTOs;

using ShelfSense.Application.Interfaces;

using ShelfSense.Domain.Entities;

using ShelfSense.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

public class StockRequestRepository : IStockRequest

{

    private readonly ShelfSenseDbContext _context;

    private readonly IReplenishmentAlert _alertRepository;

    private readonly IDeliveryStatusLog _statusLogRepository;

    public StockRequestRepository(

        ShelfSenseDbContext context,

        IReplenishmentAlert alertRepository,

        IDeliveryStatusLog statusLogRepository)

    {

        _context = context;

        _alertRepository = alertRepository;

        _statusLogRepository = statusLogRepository;

    }

    public IQueryable<StockRequest> GetAll() => _context.StockRequests.AsQueryable();

    public async Task<StockRequest?> GetByIdAsync(long id) =>

        await _context.StockRequests.FindAsync(id);

    public async Task AddAsync(StockRequest entity)

    {

        await _context.StockRequests.AddAsync(entity);

        await _context.SaveChangesAsync();

    }

    public async Task UpdateAsync(StockRequest entity)

    {

        _context.StockRequests.Update(entity);

        await _context.SaveChangesAsync();

    }

    public async Task DeleteAsync(long id)

    {

        var entity = await _context.StockRequests.FindAsync(id);

        if (entity != null)

        {

            _context.StockRequests.Remove(entity);

            await _context.SaveChangesAsync();

        }

    }

    public async Task<List<StockRequestResponse>> CreateRequestFromAlertsByUrgenyAsync()

    {

        var alerts = await _context.ReplenishmentAlerts   // Giving priority to urgent alerts

            .Where(a => a.Status == "open")

            .OrderByDescending(a =>

                a.UrgencyLevel == "critical" ? 4 :

                a.UrgencyLevel == "high" ? 3 :

                a.UrgencyLevel == "medium" ? 2 :

                a.UrgencyLevel == "low" ? 1 : 0)

            .ToListAsync();

        var responses = new List<StockRequestResponse>();

        foreach (var alert in alerts)

        {

            var productShelf = await _context.ProductShelves

                .Include(ps => ps.Shelf)

                .FirstOrDefaultAsync(ps => ps.ProductId == alert.ProductId && ps.ShelfId == alert.ShelfId);

            if (productShelf == null || productShelf.Shelf == null) continue;

            int quantityToOrder = productShelf.Shelf.Capacity - productShelf.Quantity;

            if (quantityToOrder <= 0) continue;

            var request = new StockRequest

            {

                AlertId = alert.AlertId,

                StoreId = productShelf.Shelf.StoreId,

                ProductId = alert.ProductId,

                Quantity = quantityToOrder,

                RequestDate = DateTime.UtcNow,

                RequestedDeliveryDate = CalculateRequestedDate(alert.UrgencyLevel),

                DeliveryStatus = "requested",

                EstimatedTimeOfArrival = CalculateEta("requested")

            };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            responses.Add(new StockRequestResponse

            {

                AlertId = alert.AlertId,

                RequestId = request.RequestId,

                StoreId = request.StoreId,

                ProductId = request.ProductId,

                Quantity = request.Quantity,

                DeliveryStatus = request.DeliveryStatus,

                RequestDate = request.RequestDate,

                RequestedDeliveryDate = request.RequestedDeliveryDate,

                EstimatedTimeOfArrival = request.EstimatedTimeOfArrival

            });

            alert.Status = "acknowledged";

            await _alertRepository.UpdateAsync(alert);

        }

        await _context.SaveChangesAsync();

        return responses;

    }

    public async Task UpdateDeliveryStatusAsync(long requestId, string deliveryStatus, DateTime? eta = null)

    {

        var request = await _context.StockRequests

            .Include(r => r.Product)

            .Include(r => r.Store)

            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null)

            throw new Exception($"StockRequest ID {requestId} not found.");

        request.DeliveryStatus = deliveryStatus;

        request.EstimatedTimeOfArrival = eta ?? deliveryStatus switch

        {

            "requested" => DateTime.UtcNow.AddDays(2),

            "in_transit" => DateTime.UtcNow.AddDays(1),

            "delivered" => DateTime.UtcNow,

            "cancelled" => null,

            _ => null

        };

        if (deliveryStatus == "delivered")

        {

            if (request.AlertId.HasValue)

            {

                var alert = await _alertRepository.GetByIdAsync(request.AlertId.Value);

                if (alert != null && alert.Status == "acknowledged")

                {

                    alert.Status = "resolved";

                    await _alertRepository.UpdateAsync(alert);

                    _context.ReplenishmentAlerts.Remove(alert); // ✅ Remove resolved alert

                }

            }

            var delivered = new DeliveredStockRequest

            {

                OriginalRequestId = request.RequestId,

                ProductId = request.ProductId,

                StoreId = request.StoreId,

                Quantity = request.Quantity,

                DeliveredAt = DateTime.UtcNow,

                AlertId = request.AlertId

            };

            await _context.DeliveredStockRequests.AddAsync(delivered);

            // Optional: remove original request if archiving

            // _context.StockRequests.Remove(request);

        }

        if (deliveryStatus == "cancelled")

        {

            request.EstimatedTimeOfArrival = null;

        }

        _context.StockRequests.Update(request);

        await _statusLogRepository.AddAsync(new DeliveryStatusLog

        {

            RequestId = request.RequestId,

            AlertId = request.AlertId,

            DeliveryStatus = deliveryStatus,

            StatusChangedAt = DateTime.UtcNow

        });

        await _context.SaveChangesAsync();

    }

    private DateTime? CalculateEta(string status)

    {

        return status switch

        {

            "requested" => DateTime.UtcNow.AddDays(2),

            "in_transit" => DateTime.UtcNow.AddDays(1),

            "delivered" => DateTime.UtcNow,

            "cancelled" => null,

            _ => null

        };

    }

    private DateTime CalculateRequestedDate(string urgency)

    {

        return urgency switch

        {

            "critical" => DateTime.UtcNow.AddDays(1),

            "high" => DateTime.UtcNow.AddDays(2),

            "medium" => DateTime.UtcNow.AddDays(3),

            "low" => DateTime.UtcNow.AddDays(4),

            _ => DateTime.UtcNow.AddDays(2)

        };

    }

}
