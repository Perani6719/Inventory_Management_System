using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;
using Xunit;

public class RestockTaskRepositoryTests
{
    private ShelfSenseDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ShelfSenseDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        var context = GetDbContext();
        context.RestockTasks.AddRange(
            new RestockTask { TaskId = 1, Status = "pending" },
            new RestockTask { TaskId = 2, Status = "completed" }
        );
        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);
        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectTask()
    {
        var context = GetDbContext();
        context.RestockTasks.Add(new RestockTask { TaskId = 10, Status = "pending" });
        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);
        var result = await repo.GetByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result.TaskId);
    }

    [Fact]
    public async Task GetByStaffIdAsync_ReturnsTasksForStaff()
    {
        var context = GetDbContext();
        context.RestockTasks.AddRange(
            new RestockTask { TaskId = 1, AssignedTo = 100 },
            new RestockTask { TaskId = 2, AssignedTo = 101 },
            new RestockTask { TaskId = 3, AssignedTo = 100 }
        );
        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);
        var result = await repo.GetByStaffIdAsync(100);

        Assert.Equal(2, result.Count);
    }



    [Fact]
    public async Task CheckStatusByIdAsync_ReturnsCorrectStatus()
    {
        var context = GetDbContext();
        context.RestockTasks.Add(new RestockTask
        {
            TaskId = 1,
            AssignedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);
        var status = await repo.CheckStatusByIdAsync(1);

        Assert.Equal("Task completed without delay", status);
    }

    [Fact]
    public async Task AssignTasksFromDeliveredStockAsync_AssignsTasksCorrectly()
    {
        var context = GetDbContext();

        context.Staffs.Add(new Staff { StaffId = 1, Name = "Staff A" });
        context.ProductShelves.Add(new ProductShelf { ProductId = 101, ShelfId = 201, Quantity = 0 });
        context.DeliveredStockRequests.Add(new DeliveredStockRequest { ProductId = 101, Quantity = 10 });

        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);
        await repo.AssignTasksFromDeliveredStockAsync();

        var tasks = await context.RestockTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(101, tasks[0].ProductId);
        Assert.Equal("pending", tasks[0].Status);
    }

     
    public async Task OrganizeDeliveredProductAsync_UpdatesShelfAndTaskStatus()
    {
        // Arrange
        var context = GetDbContext(); // Your in-memory DbContext setup
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowIST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

        var staff = new Staff { StaffId = 1, Name = "Staff A" };
        var productShelf = new ProductShelf
        {
            ProductId = 101,
            ShelfId = 201,
            Quantity = 5,
            LastRestockedAt = nowIST.AddDays(-1) // ✅ IST
        };
        var deliveredItem = new DeliveredStockRequest
        {
            ProductId = 101,
            Quantity = 10,
            IsProcessed = false
        };
        var restockTask = new RestockTask
        {
            TaskId = 1,
            ProductId = 101,
            ShelfId = 201,
            AssignedTo = 1,
            AssignedAt = nowIST.AddMinutes(-30), // ✅ IST
            Status = "pending"
        };

        context.Staffs.Add(staff);
        context.ProductShelves.Add(productShelf);
        context.DeliveredStockRequests.Add(deliveredItem);
        context.RestockTasks.Add(restockTask);
        await context.SaveChangesAsync();

        var repo = new RestockTaskRepository(context);

        // Act
        var result = await repo.OrganizeDeliveredProductAsync(1, 1);

        // Assert
        var updatedShelf = await context.ProductShelves.FirstAsync();
        var updatedTask = await context.RestockTasks.FirstAsync();
        var updatedDeliveredItem = await context.DeliveredStockRequests.FirstAsync();

        Assert.Equal(15, updatedShelf.Quantity); // 5 + 10
        Assert.Equal("completed", updatedTask.Status); // ✅ Should now pass
        Assert.True(updatedDeliveredItem.IsProcessed);
        Assert.Equal("Task completed without delay", result);
    }
}
