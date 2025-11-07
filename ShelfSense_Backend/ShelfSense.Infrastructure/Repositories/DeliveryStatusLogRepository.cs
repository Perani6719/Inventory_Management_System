
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Infrastructure.Repositories
{
    public class DeliveryStatusLogRepository : IDeliveryStatusLog
    {
        private readonly ShelfSenseDbContext _context;

        public DeliveryStatusLogRepository(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public async Task<List<DeliveryStatusLog>> GetAllAsync()
        {
            return await _context.DeliveryStatusLogs
                .OrderByDescending(d => d.StatusChangedAt)
                .ToListAsync();
        }

        public async Task AddAsync(DeliveryStatusLog log)
        {
            var existingLog = await _context.DeliveryStatusLogs
                .FirstOrDefaultAsync(d => d.RequestId == log.RequestId);

            if (existingLog != null)
            {
                // Update existing log entry
                existingLog.DeliveryStatus = log.DeliveryStatus;
                existingLog.StatusChangedAt = log.StatusChangedAt;
                _context.DeliveryStatusLogs.Update(existingLog);
            }
            else
            {
                // Create new log entry
                await _context.DeliveryStatusLogs.AddAsync(log);
            }

            await _context.SaveChangesAsync();
        }
    }
}
