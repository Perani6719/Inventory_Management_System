using Xunit;

using Moq;

using Microsoft.EntityFrameworkCore;

using ShelfSense.Infrastructure.Data;

using ShelfSense.Application.Interfaces;

using ShelfSense.Domain.Entities;

using ShelfSense.Application.DTOs;

using System.Threading.Tasks;

using System.Collections.Generic;

using System.Linq;

using System;


namespace ShelfSense.Tests

{

    public class StockRequestRepositoryTests

    {

        private readonly ShelfSenseDbContext _context;

        private readonly Mock<IReplenishmentAlert> _alertRepoMock;

        private readonly Mock<IDeliveryStatusLog> _statusLogMock;

        private readonly StockRequestRepository _repository;

        public StockRequestRepositoryTests()

        {

            var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()

                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())

                .Options;

            _context = new ShelfSenseDbContext(options);

            _alertRepoMock = new Mock<IReplenishmentAlert>();

            _statusLogMock = new Mock<IDeliveryStatusLog>();

            _repository = new StockRequestRepository(_context, _alertRepoMock.Object, _statusLogMock.Object);

        }

        [Fact]

        public async Task AddAsync_ShouldAddStockRequest()

        {

            var request = new StockRequest

            {

                ProductId = 1,

                StoreId = 1,

                Quantity = 10,

                RequestDate = DateTime.UtcNow,

                DeliveryStatus = "requested"

            };

            await _repository.AddAsync(request);

            var result = await _context.StockRequests.FindAsync(request.RequestId);

            Assert.NotNull(result);

            Assert.Equal("requested", result.DeliveryStatus);

        }

        [Fact]

        public void GetAll_ShouldReturnQueryable()

        {

            var result = _repository.GetAll();

            Assert.IsAssignableFrom<IQueryable<StockRequest>>(result);

        }

        [Fact]

        public async Task GetByIdAsync_ShouldReturnCorrectEntity()

        {

            var request = new StockRequest { ProductId = 2, StoreId = 2, Quantity = 5 };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(request.RequestId);

            Assert.NotNull(result);

            Assert.Equal(2, result.ProductId);

        }

        [Fact]

        public async Task UpdateAsync_ShouldModifyEntity()

        {

            var request = new StockRequest { ProductId = 5, StoreId = 5, Quantity = 30, DeliveryStatus = "requested" };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            request.Quantity = 50;

            await _repository.UpdateAsync(request);

            var updated = await _context.StockRequests.FindAsync(request.RequestId);

            Assert.Equal(50, updated.Quantity);

        }

        [Fact]

        public async Task DeleteAsync_ShouldRemoveEntity()

        {

            var request = new StockRequest { ProductId = 3, StoreId = 3, Quantity = 15 };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(request.RequestId);

            var result = await _context.StockRequests.FindAsync(request.RequestId);

            Assert.Null(result);

        }

        [Fact]

        public async Task UpdateDeliveryStatusAsync_ShouldUpdateStatusAndLog()

        {

            var store = new Store { StoreId = 4, StoreName = "Test Store" };
            var product = new Product { ProductId = 4, ProductName = "Test Product", CategoryId = 1 };

            await _context.Stores.AddAsync(store);
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            var request = new StockRequest
            {
                ProductId = product.ProductId,
                StoreId = store.StoreId,
                Quantity = 20,
                DeliveryStatus = "requested",
                RequestDate = DateTime.UtcNow
            };

            await _context.StockRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            await _repository.UpdateDeliveryStatusAsync(request.RequestId, "delivered");


        }

        [Fact]

        public async Task CreateRequestFromAlertsByUrgenyAsync_ShouldCreateRequests()

        {

            var shelf = new Shelf { ShelfId = 1, StoreId = 1, Capacity = 100 };

            var productShelf = new ProductShelf { ProductId = 1, ShelfId = 1, Quantity = 20, Shelf = shelf };

            var alert = new ReplenishmentAlert { AlertId = 1, ProductId = 1, ShelfId = 1, Status = "open", UrgencyLevel = "high" };

            await _context.Shelves.AddAsync(shelf);

            await _context.ProductShelves.AddAsync(productShelf);

            await _context.ReplenishmentAlerts.AddAsync(alert);

            await _context.SaveChangesAsync();

            _alertRepoMock.Setup(x => x.UpdateAsync(It.IsAny<ReplenishmentAlert>())).Returns(Task.CompletedTask);

            var result = await _repository.CreateRequestFromAlertsByUrgenyAsync();

            Assert.Single(result);

            Assert.Equal("requested", result[0].DeliveryStatus);

            Assert.Equal(alert.AlertId, result[0].AlertId);

        }

    }

}

