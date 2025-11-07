using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShelfSense.Application.DTOs;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;
using ShelfSense.WebAPI.Controllers;
using Xunit;

namespace ShelfSense.Tests
{
    public class ProductShelfControllerTests
    {
        private ShelfSenseDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ShelfSenseDbContext(options);
        }

        private IMapper GetMockMapper()
        {
            var mockMapper = new Mock<IMapper>();
             
            mockMapper.Setup(m => m.Map<List<ProductShelfResponse>>(It.IsAny<List<ProductShelf>>()))
                      .Returns((List<ProductShelf> src) => src.Select(ps => new ProductShelfResponse
                      {
                          ProductShelfId = ps.ProductShelfId,
                          ProductId = ps.ProductId,
                          ShelfId = ps.ShelfId,
                          Quantity = ps.Quantity,
                          LastRestockedAt = ps.LastRestockedAt ?? DateTime.UtcNow
                      }).ToList());

            mockMapper.Setup(m => m.Map<ProductShelfResponse>(It.IsAny<ProductShelf>()))
                      .Returns((ProductShelf ps) => new ProductShelfResponse
                      {
                          ProductShelfId = ps.ProductShelfId,
                          ProductId = ps.ProductId,
                          ShelfId = ps.ShelfId,
                          Quantity = ps.Quantity,
                          LastRestockedAt = ps.LastRestockedAt ?? DateTime.UtcNow
                      });

            return mockMapper.Object;
        }

        private string ExtractMessage(ObjectResult result)
        {
            var json = JsonSerializer.Serialize(result.Value);  // converts object -> json
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json); // Back to object
            return dict != null && dict.ContainsKey("message") ? dict["message"]?.ToString() ?? "" : "";
        }

        [Fact]
        public async Task AutoAssign_ShouldReturnConflict_WhenProductAlreadyAssigned()
        {
            var context = GetDbContext();
            context.Products.Add(new Product { ProductId = 1, CategoryId = 1 });
            context.Shelves.Add(new Shelf { ShelfId = 1, CategoryId = 1, Capacity = 100 });
            context.ProductShelves.Add(new ProductShelf { ProductId = 1, ShelfId = 1, Quantity = 10 });
            context.SaveChanges();

            var repo = new ProductShelfRepository(context);
            var controller = new ProductShelfController(repo, context, GetMockMapper());

            var request = new ProductShelfAutoAssignRequest
            {
                ProductId = 1,
                CategoryId = 1,
                InitialQuantity = 10
            };

            var result = await controller.AutoAssign(request) as ObjectResult;   // ObjectResult == has http response and response body
            Assert.NotNull(result);
            Assert.Equal(409, result.StatusCode);
            var message = ExtractMessage(result);
            Assert.Contains("already assigned", message);
        }

        [Fact]
        public async Task AutoAssign_ShouldReturnConflict_WhenCapacityExceeded()
        {
            var context = GetDbContext();
            context.Products.Add(new Product { ProductId = 2, CategoryId = 1 });
            context.Shelves.Add(new Shelf { ShelfId = 2, CategoryId = 1, Capacity = 10 });
            context.ProductShelves.Add(new ProductShelf { ShelfId = 2, ProductId = 3, Quantity = 10 });
            context.SaveChanges();

            var repo = new ProductShelfRepository(context);
            var controller = new ProductShelfController(repo, context, GetMockMapper());

            var request = new ProductShelfAutoAssignRequest
            {
                ProductId = 2,
                CategoryId = 1,
                InitialQuantity = 5
            };

            var result = await controller.AutoAssign(request) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(409, result.StatusCode);
            var message = ExtractMessage(result);
            Assert.Contains("Capacity Exceeded", message);
        }

        [Fact]
        public async Task AutoAssign_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            var context = GetDbContext();
            context.Shelves.Add(new Shelf { ShelfId = 3, CategoryId = 1, Capacity = 100 });
            context.SaveChanges();

            var repo = new ProductShelfRepository(context);
            var controller = new ProductShelfController(repo, context, GetMockMapper());

            var request = new ProductShelfAutoAssignRequest
            {
                ProductId = 999,
                CategoryId = 1,
                InitialQuantity = 10
            };

            var result = await controller.AutoAssign(request) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
            var message = ExtractMessage(result);
            Assert.Contains("not found", message);
        }

        [Fact]
        public async Task AutoAssign_ShouldReturnCreated_AndAssignToShelfWithMostRemainingCapacity()
        {
            var context = GetDbContext();
            context.Products.Add(new Product { ProductId = 4, CategoryId = 1 });
            context.Shelves.AddRange(
                new Shelf { ShelfId = 4, CategoryId = 1, Capacity = 100 },
                new Shelf { ShelfId = 5, CategoryId = 1, Capacity = 50 }
            );
            context.ProductShelves.Add(new ProductShelf { ShelfId = 5, ProductId = 5, Quantity = 40 });
            context.SaveChanges();

            var repo = new ProductShelfRepository(context);
            var controller = new ProductShelfController(repo, context, GetMockMapper());

            var request = new ProductShelfAutoAssignRequest
            {
                ProductId = 4,
                CategoryId = 1,
                InitialQuantity = 10
            };

            var result = await controller.AutoAssign(request) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            var message = ExtractMessage(result);
            Assert.Contains("successfully assigned", message);
        }
    }
}