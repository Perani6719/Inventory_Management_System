//using System;

//using System.Collections.Generic;

//using System.Linq;

//using System.Threading.Tasks;

//using Microsoft.AspNetCore.Mvc;

//using Microsoft.EntityFrameworkCore;

//using ShelfSense.Application.DTOs;

//using ShelfSense.Application.Interfaces;

//using ShelfSense.Domain.Entities;

//using ShelfSense.Infrastructure.Data;

//namespace ShelfSense.Infrastructure.Repositories

//{

//    public class RestockTaskRepository : IRestockTaskRepository

//    {

//        private readonly ShelfSenseDbContext _context;

//        public RestockTaskRepository(ShelfSenseDbContext context)

//        {

//            _context = context;

//        }

//        public async Task<List<RestockTask>> GetAllAsync()

//        {

//            return await _context.RestockTasks.ToListAsync();

//        }

//        public async Task<RestockTask?> GetByIdAsync(long taskId)

//        {

//            return await _context.RestockTasks.FindAsync(taskId);

//        }

//        public async Task<List<RestockTask>> GetByStaffIdAsync(long staffId)

//        {

//            return await _context.RestockTasks

//                .Where(t => t.AssignedTo == staffId)

//                .OrderByDescending(t => t.AssignedAt)

//                .ToListAsync();

//        }

//        public async Task<List<RestockTask>> GetDelayedTasksAsync()

//        {

//            return await _context.RestockTasks

//                .Where(t => t.Status == "completed" && t.CompletedAt != null)

//                .Where(t => EF.Functions.DateDiffHour(t.AssignedAt, t.CompletedAt.Value) > 2)

//                .ToListAsync();

//        }

//        public async Task AssignTasksFromDeliveredStockAsync()

//        {

//            var deliveredItems = await _context.DeliveredStockRequests

//                .Where(d => !_context.RestockTasks.Any(t => t.ProductId == d.ProductId))

//                .ToListAsync();

//            var staffList = await _context.Staffs.OrderBy(s => s.StaffId).ToListAsync();

//            if (!staffList.Any()) return;

//            int staffIndex = 0;

//            foreach (var item in deliveredItems)

//            {

//                // ✅ Find ProductShelf for this product

//                var productShelf = await _context.ProductShelves

//                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId);

//                if (productShelf != null)

//                {

//                    var assignedStaff = staffList[staffIndex % staffList.Count];

//                    staffIndex++; // ✅ Round-robin staff assignment

//                    var task = new RestockTask

//                    {

//                        ProductId = item.ProductId,

//                        ShelfId = productShelf.ShelfId, // ✅ Correct shelf from ProductShelf

//                        AssignedTo = assignedStaff.StaffId,

//                        AssignedAt = DateTime.UtcNow,

//                        Status = "pending"

//                    };

//                    // ✅ Optional AlertId

//                    if (item.AlertId.HasValue && await _context.ReplenishmentAlerts.AnyAsync(a => a.AlertId == item.AlertId.Value))

//                    {

//                        task.AlertId = item.AlertId.Value;

//                    }

//                    await _context.RestockTasks.AddAsync(task);

//                }

//            }

//            await _context.SaveChangesAsync();

//        }

//        public async Task<string?> OrganizeDeliveredProductAsync(long taskId, long staffId)

//        {

//            // Fetch the specific task

//            var task = await _context.RestockTasks

//                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.AssignedTo == staffId);

//            if (task == null)

//                return "Task not found or not assigned to this staff.";

//            // Find the delivered item for this product

//            var deliveredItem = await _context.DeliveredStockRequests

//                .FirstOrDefaultAsync(d => d.ProductId == task.ProductId && !d.IsProcessed);

//            if (deliveredItem == null)

//                return "No delivered stock found for this product.";

//            // Find the ProductShelf for this product and shelf

//            var productShelf = await _context.ProductShelves

//                .FirstOrDefaultAsync(ps => ps.ProductId == task.ProductId && ps.ShelfId == task.ShelfId);

//            if (productShelf == null)

//                return "Product shelf not found.";

//            // Update shelf

//            productShelf.Quantity += deliveredItem.Quantity;

//            productShelf.LastRestockedAt = DateTime.UtcNow;

//            // Update task status

//            task.CompletedAt = DateTime.UtcNow;

//            var timeDiff = task.CompletedAt.Value - task.AssignedAt;

//            task.Status = timeDiff.TotalHours <= 2 ? "completed" : "delayed";

//            // Mark delivered item as processed

//            deliveredItem.IsProcessed = true;

//            await _context.SaveChangesAsync();

//            return task.Status == "completed"

//                ? "Task completed without delay"

//                : "Task completed with delay";

//        }


//        //update status

//        public async Task<string?> CheckStatusByIdAsync(long taskId)

//        {

//            var task = await _context.RestockTasks.FindAsync(taskId);

//            if (task == null || task.CompletedAt == null || task.AssignedAt == null)

//                return null;

//            var duration = task.CompletedAt.Value - task.AssignedAt;

//            task.Status = duration.TotalHours <= 2 ? "completed" : "delayed";

//            await _context.SaveChangesAsync();

//            return task.Status == "completed"

//                ? "Task completed without delay"

//                : "Task completed with delay";

//        }


//    }

//}


using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using ShelfSense.Application.DTOs;

using ShelfSense.Application.Interfaces;

using ShelfSense.Domain.Entities;

using ShelfSense.Infrastructure.Data;

namespace ShelfSense.Infrastructure.Repositories

{

    public class RestockTaskRepository : IRestockTaskRepository

    {

        private readonly ShelfSenseDbContext _context;

        public RestockTaskRepository(ShelfSenseDbContext context)

        {

            _context = context;

        }

        public async Task<List<RestockTask>> GetAllAsync()

        {

            return await _context.RestockTasks.ToListAsync();

        }

        public async Task<RestockTask?> GetByIdAsync(long taskId)

        {

            return await _context.RestockTasks.FindAsync(taskId);

        }

        public async Task<List<RestockTask>> GetByStaffIdAsync(long staffId)

        {

            return await _context.RestockTasks

                .Where(t => t.AssignedTo == staffId)

                .OrderByDescending(t => t.AssignedAt)

                .ToListAsync();

        }

        //get delayed tasks

        public async Task<List<RestockTask>> GetDelayedTasksAsync()

        {

            return await _context.RestockTasks

                .Where(t => t.Status == "delayed" && t.CompletedAt != null)

                .ToListAsync();

        }


        //assign tasks from delivered stock

        public async Task<string> AssignTasksFromDeliveredStockAsync()

        {

            var deliveredItems = await _context.DeliveredStockRequests

                .Where(d => !_context.RestockTasks.Any(t => t.ProductId == d.ProductId))

                .ToListAsync();

            if (!deliveredItems.Any())

            {

                return "There are no delivered products present now to assign tasks.";

            }

            var staffList = await _context.Staffs.OrderBy(s => s.StaffId).ToListAsync();

            if (!staffList.Any()) return "No staff available for assignment.";

            int staffIndex = 0;

            // ✅ Declare IST TimeZone once

            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            foreach (var item in deliveredItems)

            {

                var productShelf = await _context.ProductShelves

                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId);

                if (productShelf != null)

                {

                    var assignedStaff = staffList[staffIndex % staffList.Count];

                    staffIndex++;

                    var task = new RestockTask

                    {

                        ProductId = item.ProductId,

                        ShelfId = productShelf.ShelfId,

                        AssignedTo = assignedStaff.StaffId,

                        AssignedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone), // ✅ IST time

                        Status = "pending",

                        QuantityRestocked = item.Quantity

                    };

                    if (item.AlertId.HasValue && await _context.ReplenishmentAlerts.AnyAsync(a => a.AlertId == item.AlertId.Value))

                    {

                        task.AlertId = item.AlertId.Value;

                    }

                    await _context.RestockTasks.AddAsync(task);

                    item.IsProcessed = false;

                    _context.DeliveredStockRequests.Update(item);

                }

            }

            await _context.SaveChangesAsync();

            return $"{deliveredItems.Count} restock tasks assigned successfully.";

        }


        //organizing the delivered products

        public async Task<string?> OrganizeDeliveredProductAsync(long taskId, long staffId)

        {

            // Fetch the specific task

            var task = await _context.RestockTasks

                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.AssignedTo == staffId);

            if (task == null)

                return "Task not found or not assigned to this staff.";

            // Find the delivered item for this product

            var deliveredItem = await _context.DeliveredStockRequests

                .FirstOrDefaultAsync(d => d.ProductId == task.ProductId && !d.IsProcessed);

            if (deliveredItem == null)

                return "No delivered stock found for this product.";

            // Find the ProductShelf for this product and shelf

            var productShelf = await _context.ProductShelves

                .FirstOrDefaultAsync(ps => ps.ProductId == task.ProductId && ps.ShelfId == task.ShelfId);

            if (productShelf == null)

                return "Product shelf not found.";

            // Update shelf


            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            productShelf.Quantity += deliveredItem.Quantity;

            productShelf.LastRestockedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone); // ✅ IST

            // Update task status

            task.CompletedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone); // ✅ IST

            var timeDiff = task.CompletedAt.Value - task.AssignedAt;

            task.Status = timeDiff.TotalHours <= 2 ? "completed" : "delayed";

            // Mark delivered item as processed

            deliveredItem.IsProcessed = true;

            await _context.SaveChangesAsync();

            return task.Status == "completed"

                ? "Task completed without delay"

                : "Task completed with delay";

        }


        //Check task status using id

        public async Task<string?> CheckStatusByIdAsync(long taskId)

        {

            var task = await _context.RestockTasks.FindAsync(taskId);

            if (task == null || task.CompletedAt == null || task.AssignedAt == null)

                return null;

            var duration = task.CompletedAt.Value - task.AssignedAt;

            task.Status = duration.TotalHours <= 2 ? "completed" : "delayed";

            await _context.SaveChangesAsync();

            return task.Status == "completed"

                ? "Task completed without delay"

                : "Task completed with delay";

        }



    }

}

