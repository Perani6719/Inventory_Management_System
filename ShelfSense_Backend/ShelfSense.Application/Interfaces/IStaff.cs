using ShelfSense.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IStaffRepository
    {
        IQueryable<Staff> GetAll();
        Task<Staff?> GetByIdAsync(long id);
        Task AddAsync(Staff entity);
        Task UpdateAsync(Staff entity);
        Task DeleteAsync(long id);
    }
}