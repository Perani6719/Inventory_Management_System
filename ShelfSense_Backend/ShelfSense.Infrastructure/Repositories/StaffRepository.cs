using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data; // Assuming DbContext is here
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Infrastructure.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly ShelfSenseDbContext _context;

        public StaffRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public IQueryable<Staff> GetAll() => _context.Set<Staff>().AsQueryable();

        public async Task<Staff?> GetByIdAsync(long id) =>
            await _context.Set<Staff>().FindAsync(id);

        public async Task AddAsync(Staff entity)
        {
            await _context.Set<Staff>().AddAsync(entity);
            // 🔑 CRITICAL: This line commits the entity to the database.
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Staff entity)
        {
            _context.Set<Staff>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Set<Staff>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<Staff>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}