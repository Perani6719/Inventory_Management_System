// SHELFSENSE.APPLICATION.INTERFACES/IProductShelfRepository.cs

using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IProductShelfRepository
    {
        IQueryable<ProductShelf> GetAll();
        Task<ProductShelf?> GetByIdAsync(long id);
        Task AddAsync(ProductShelf entity);
        Task UpdateAsync(ProductShelf entity);
        Task DeleteAsync(long id);

        // Existing method to get all active alerts
        Task<IQueryable<ReplenishmentAlert>> GetActiveAlertsAsync();

        // Method for Hangfire job: returns NEWLY CREATED alerts
        Task<IEnumerable<ReplenishmentAlert>> RunPredictionAndGenerateAlertsAsync();
    }
}