using Xunit;

using Moq;

using AutoMapper;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using ShelfSense.WebAPI.Controllers;

using ShelfSense.Infrastructure.Data;

using ShelfSense.Application.Interfaces;

using ShelfSense.Domain.Entities;

using ShelfSense.Application.DTOs;

using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;

namespace ShelfSense.Tests

{

    public class WarehouseControllerTests

    {

        private readonly ShelfSenseDbContext _context;

        private readonly IMapper _mapper;

        private readonly Mock<IDeliveryStatusLog> _statusLogMock;

        private readonly Mock<IStockRequest> _stockRequestMock;

        private readonly WarehouseController _controller;

        public WarehouseControllerTests()

        {

            var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()

                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())

                .Options;

            _context = new ShelfSenseDbContext(options);

            var config = new MapperConfiguration(cfg =>

            {

                cfg.CreateMap<StockRequest, StockRequestResponse>();

            });

            _mapper = config.CreateMapper();

            _statusLogMock = new Mock<IDeliveryStatusLog>();

            _stockRequestMock = new Mock<IStockRequest>();

            _controller = new WarehouseController(_context, _mapper, _statusLogMock.Object, _stockRequestMock.Object);

        }

        [Fact]

        public async Task GetIncomingRequests_ShouldReturnOk()

        {

            var product = new Product { ProductId = 1, ProductName = "TestProduct" };

            var store = new Store { StoreId = 1, StoreName = "TestStore" };

            var alert = new ReplenishmentAlert { AlertId = 1, UrgencyLevel = "high", PredictedDepletionDate = DateTime.UtcNow.AddDays(1) };

            var request = new StockRequest

            {

                RequestId = 1,

                Product = product,

                Store = store,

                Alert = alert,

                DeliveryStatus = "requested",

                RequestDate = DateTime.UtcNow

            };

            await _context.Products.AddAsync(product);

            await _context.Stores.AddAsync(store);

            await _context.ReplenishmentAlerts.AddAsync(alert);

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            var result = await _controller.GetIncomingRequests();

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Contains("Incoming stock requests", okResult.Value.ToString());

        }

        [Fact]

        public async Task MarkAsInTransit_ShouldReturnOk()

        {

            var request = new StockRequest { RequestId = 2, DeliveryStatus = "requested" };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            var result = await _controller.MarkAsInTransit(2, DateTime.UtcNow.AddDays(1));

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Contains("marked as in transit", okResult.Value.ToString());

        }

        [Fact]

        public async Task MarkAsDelivered_ShouldReturnOk()

        {

            var product = new Product { ProductId = 2, ProductName = "TestProduct2" };

            var store = new Store { StoreId = 2, StoreName = "TestStore2" };

            var request = new StockRequest

            {

                RequestId = 3,

                ProductId = 2,

                StoreId = 2,

                Product = product,

                Store = store,

                DeliveryStatus = "in_transit"

            };

            await _context.Products.AddAsync(product);

            await _context.Stores.AddAsync(store);

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            _stockRequestMock

                .Setup(x => x.UpdateDeliveryStatusAsync(3, "delivered", It.IsAny<DateTime?>()))

                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAsDelivered(3);

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Contains("marked as delivered", okResult.Value.ToString());

        }

        [Fact]

        public async Task CancelRequest_ShouldReturnOk()

        {

            var request = new StockRequest { RequestId = 4, DeliveryStatus = "requested" };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            var result = await _controller.CancelRequest(4, "Out of stock");

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Contains("has been cancelled", okResult.Value.ToString());

        }

        [Fact]

        public async Task GetPendingRequests_ShouldReturnOk()

        {

            var request = new StockRequest { RequestId = 5, DeliveryStatus = "requested" };

            await _context.StockRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            var result = await _controller.GetPendingRequests();

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Contains("Pending stock requests", okResult.Value.ToString());

        }

    }

}

