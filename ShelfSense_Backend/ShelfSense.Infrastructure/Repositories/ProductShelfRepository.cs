// SHELFSENSE.INFRASTRUCTURE.REPOSITORIES/ProductShelfRepository.cs

using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

public class ProductShelfRepository : IProductShelfRepository
{
    private readonly ShelfSenseDbContext _context;

    public ProductShelfRepository(ShelfSenseDbContext context)
    {
        _context = context;
    }

    // Existing CRUD methods (GetAll, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync)
    public IQueryable<ProductShelf> GetAll() => _context.ProductShelves.AsQueryable();   // AsQuerable == Used for deferred execution
    public async Task<ProductShelf?> GetByIdAsync(long id) => await _context.ProductShelves.FindAsync(id);
    public async Task AddAsync(ProductShelf entity) { await _context.ProductShelves.AddAsync(entity); await _context.SaveChangesAsync(); }
    public async Task UpdateAsync(ProductShelf entity) { _context.ProductShelves.Update(entity); await _context.SaveChangesAsync(); }
    public async Task DeleteAsync(long id)
    {
        var entity = await _context.ProductShelves.FindAsync(id);
        if (entity != null) { _context.ProductShelves.Remove(entity); await _context.SaveChangesAsync(); }
    }

    // =======================================================
    // IMPLEMENTATION: GetActiveAlertsAsync (Used for email context)
    // =======================================================
    public async Task<IQueryable<ReplenishmentAlert>> GetActiveAlertsAsync()
    {
        // Includes Shelf and Product to allow filtering by StoreId and building email content
        var activeAlerts = _context.ReplenishmentAlerts
            .Include(a => a.ProductShelf)
            .Include(a => a.Shelf) // Crucial for getting Shelf.StoreId
            .AsQueryable();

        return activeAlerts;
    }

    // =======================================================
    // IMPLEMENTATION: RunPredictionAndGenerateAlertsAsync (Used by Hangfire)
    // =======================================================
    public async Task<IEnumerable<ReplenishmentAlert>> RunPredictionAndGenerateAlertsAsync()
    {
       
        await Task.Delay(100); // Simulate database/calculation work


        var alertsToNotify = await _context.ReplenishmentAlerts
            .Include(a => a.Shelf) // Ensure Shelf is included
            .Take(2)
            .ToListAsync(); 

        return alertsToNotify;
        
    }
}