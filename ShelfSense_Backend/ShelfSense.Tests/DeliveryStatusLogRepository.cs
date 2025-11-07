using Xunit;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;
using ShelfSense.Domain.Entities;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ShelfSense.Tests
{
    public class DeliveryStatusLogRepositoryTests
    {
        private readonly ShelfSenseDbContext _context;
        private readonly DeliveryStatusLogRepository _repository;

        public DeliveryStatusLogRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ShelfSenseDbContext(options);
            _repository = new DeliveryStatusLogRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldCreateNewLog_WhenNoExistingLog()
        {
            var log = new DeliveryStatusLog
            {
                RequestId = 1,
                AlertId = 10,
                DeliveryStatus = "in_transit",
                StatusChangedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(log);

            var savedLog = await _context.DeliveryStatusLogs.FirstOrDefaultAsync(d => d.RequestId == 1);
            Assert.NotNull(savedLog);
            Assert.Equal("in_transit", savedLog.DeliveryStatus);
        }

        [Fact]
        public async Task AddAsync_ShouldUpdateExistingLog_WhenLogExists()
        {
            var initialLog = new DeliveryStatusLog
            {
                RequestId = 2,
                AlertId = 20,
                DeliveryStatus = "requested",
                StatusChangedAt = DateTime.UtcNow.AddHours(-1)
            };

            await _context.DeliveryStatusLogs.AddAsync(initialLog);
            await _context.SaveChangesAsync();

            var updatedLog = new DeliveryStatusLog
            {
                RequestId = 2,
                AlertId = 20,
                DeliveryStatus = "delivered",
                StatusChangedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(updatedLog);

            var savedLog = await _context.DeliveryStatusLogs.FirstOrDefaultAsync(d => d.RequestId == 2);
            Assert.NotNull(savedLog);
            Assert.Equal("delivered", savedLog.DeliveryStatus);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnLogsInDescendingOrder()
        {
            await _context.DeliveryStatusLogs.AddRangeAsync(
                new DeliveryStatusLog { RequestId = 3, StatusChangedAt = DateTime.UtcNow.AddMinutes(-10), DeliveryStatus = "requested" },
                new DeliveryStatusLog { RequestId = 4, StatusChangedAt = DateTime.UtcNow, DeliveryStatus = "in_transit" }
            );
            await _context.SaveChangesAsync();

            var logs = await _repository.GetAllAsync();

            Assert.Equal(2, logs.Count);
            Assert.Equal(4, logs.First().RequestId); // Most recent first
        }
    }
}