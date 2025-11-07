using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.Interfaces
{
    public interface IProductRepository
    {
        IQueryable<Product> GetAllProducts();
        Task<Product?> GetProductByIdAsync(long id);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(long id);
    }
}
