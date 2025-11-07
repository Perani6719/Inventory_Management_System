using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System.Linq; // Added for Linq

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductShelfController : ControllerBase
    {
        private readonly IProductShelfRepository _repository;
        private readonly ShelfSenseDbContext _dbContext;
        private readonly IMapper _mapper;

        public ProductShelfController(IProductShelfRepository repository, ShelfSenseDbContext dbContext, IMapper mapper)
        {
            _repository = repository;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        // 🔓 Accessible to all authenticated users
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var items = _repository.GetAll().ToList();
                var response = _mapper.Map<List<ProductShelfResponse>>(items);
                return Ok(new { message = "ProductShelves retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving ProductShelves.", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(id);
                if (item == null)
                    return NotFound(new { message = $"ProductShelf with ID {id} not found." });

                var response = _mapper.Map<ProductShelfResponse>(item);
                return Ok(new { message = "ProductShelf retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving ProductShelf {id}.", details = ex.Message });
            }
        }

        // 🔐 Manager-only - NEW ENDPOINT FOR AUTOMATIC ASSIGNMENT
        /// <summary>
        /// Automatically assigns a newly created product to an available shelf matching its category.
        /// </summary>
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost("auto-assign")]
        public async Task<IActionResult> AutoAssign([FromBody] ProductShelfAutoAssignRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(new { message = "Invalid request or missing ProductId/CategoryId/InitialQuantity." });

            try
            {
                // 1. Fetch product and validate existence
                var product = await _dbContext.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { message = $"Product ID '{request.ProductId}' not found." });

                // 2. Validate that the incoming CategoryId matches the product's actual CategoryId
                if (product.CategoryId != request.CategoryId)
                {
                    return BadRequest(new
                    {
                        message = $"Category mismatch. Product '{product.ProductName}' (ID: {product.ProductId}) belongs to Category ID {product.CategoryId}, not {request.CategoryId}."
                    });
                }

                // 3. Check if already the same product is present in the Shelf
                var existingAssignment = await _dbContext.ProductShelves
                    .FirstOrDefaultAsync(ps => ps.ProductId == request.ProductId);

                if (existingAssignment != null)
                {
                    return Conflict(new { message = $"Product ID '{request.ProductId}' is already assigned to a shelf." });
                }

                // 4. Find shelves designated for the product's actual category
                var shelfStocks = await _dbContext.Shelves
                    .Where(s => s.CategoryId == product.CategoryId)  
                    .Select(s => new
                    {
                        Shelf = s,
                        CurrentStock = s.ProductShelves.Sum(ps => (int?)ps.Quantity) ?? 0,
                        IsCategoryPure = !s.ProductShelves.Any() ||
                                         s.ProductShelves.All(ps => ps.Product.CategoryId == product.CategoryId)
                    })
                    .ToListAsync();

                var potentialShelves = shelfStocks
                    .Where(s => s.IsCategoryPure)
                    .Where(s => s.Shelf.Capacity >= (s.CurrentStock + request.InitialQuantity))
                    .OrderByDescending(s => s.Shelf.Capacity - s.CurrentStock)
                    .ToList();

                var bestShelfAssignment = potentialShelves.FirstOrDefault();

                if (bestShelfAssignment == null)
                {
                    return Conflict(new
                    {
                        message = $"Capacity Exceeded or Category Conflict. No available shelf found for Category ID {product.CategoryId} that can accommodate {request.InitialQuantity} units while maintaining category purity."
                    });
                }

                var availableShelf = bestShelfAssignment.Shelf;
                int newTotalStock = bestShelfAssignment.CurrentStock + request.InitialQuantity;

                var productShelfEntry = new ProductShelf
                {
                    ProductId = product.ProductId,
                    ShelfId = availableShelf.ShelfId,
                    Quantity = request.InitialQuantity,
                    LastRestockedAt = request.InitialQuantity > 0 ? DateTime.UtcNow : (DateTime?)null
                };

                await _repository.AddAsync(productShelfEntry);
                var response = _mapper.Map<ProductShelfResponse>(productShelfEntry);

                return CreatedAtAction(nameof(GetById), new { id = response.ProductShelfId }, new
                {
                    message = $"Product '{product.ProductName}' (ID: {product.ProductId}) successfully assigned to Shelf '{availableShelf.ShelfCode}' (ID: {availableShelf.ShelfId}) with an initial stock of {request.InitialQuantity}. New total stock on shelf: {newTotalStock}/{availableShelf.Capacity}.",
                    data = response
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
            {
                return Conflict(new { message = "This product is already assigned to this shelf (a unique constraint violation occurred)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during automatic shelf assignment.", details = ex.Message });
            }
        }


        // 🔐 Manager-only (Manual Create - Existing method)
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductShelfCreateRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body cannot be null." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var product = await _dbContext.Products.FindAsync(request.ProductId);
                var shelf = await _dbContext.Shelves.FindAsync(request.ShelfId);

                if (product == null)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                if (shelf == null)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                if (product.CategoryId != shelf.CategoryId)
                    return BadRequest(new { message = "Product and shelf categories must match." });

                var entity = _mapper.Map<ProductShelf>(request);

                try
                {
                    await _repository.AddAsync(entity);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
                {
                    return Conflict(new { message = "This product is already assigned to this shelf." });
                }

                var response = _mapper.Map<ProductShelfResponse>(entity);
                return CreatedAtAction(nameof(GetById), new { id = response.ProductShelfId }, new
                {
                    message = "Product assigned to shelf successfully.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating ProductShelf.", details = ex.Message });
            }

        }

        // 🔐 Manager-only (Update - Existing method)
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProductShelfCreateRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body cannot be null." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"ProductShelf with ID {id} not found." });

                var product = await _dbContext.Products.FindAsync(request.ProductId);
                var shelf = await _dbContext.Shelves.FindAsync(request.ShelfId);

                if (product == null)
                    return BadRequest(new { message = $"Product ID '{request.ProductId}' does not exist." });

                if (shelf == null)
                    return BadRequest(new { message = $"Shelf ID '{request.ShelfId}' does not exist." });

                if (product.CategoryId != shelf.CategoryId)
                    return BadRequest(new { message = "Product and shelf categories must match." });

                existing.ProductId = request.ProductId;
                existing.ShelfId = request.ShelfId;
                existing.Quantity = request.Quantity;
                existing.LastRestockedAt = DateTime.UtcNow; // Update restock time on quantity change

                try
                {
                    await _repository.UpdateAsync(existing);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ProductShelves_ProductId_ShelfId") == true)
                {
                    return Conflict(new { message = "This product is already assigned to this shelf." });
                }

                return Ok(new { message = $"ProductShelf with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating ProductShelf {id}.", details = ex.Message });
            }

        }

        // 🔐 Manager-only with confirmation and constraint handling (Delete - Existing method)
        [Authorize(Roles = "admin,manager,staff")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            try
            {
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                    });
                }

                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"ProductShelf with ID {id} not found." });

                try
                {
                    await _repository.DeleteAsync(id);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    return Conflict(new
                    {
                        message = $"Cannot delete ProductShelf ID {id} because it is referenced in other records."
                    });
                }

                return Ok(new { message = $"ProductShelf with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Unexpected error while deleting ProductShelf {id}.",
                    details = ex.Message
                });
            }
        }


       
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("metrics")]
        public async Task<IActionResult> GetShelfMetrics()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                // CHANGE: Set the window to 30 days ago 
                var thirtyDaysAgo = today.AddDays(-30);

                // --- 1. Pre-calculate aggregate data from ProductShelves (Runs in SQL) ---
                var psAggregates = await _dbContext.ProductShelves
                    .GroupBy(ps => ps.ShelfId)
                    .Select(g => new
                    {
                        ShelfId = g.Key,
                        CurrentStock = g.Sum(ps => (int?)ps.Quantity) ?? 0,
                        TotalProductsAssigned = g.Count(),
                        // CHANGE: Calculate Restock Count for the last 30 days
                        RestockCountLast30Days = g.Count(ps =>
                            ps.LastRestockedAt.HasValue && ps.LastRestockedAt.Value.Date >= thirtyDaysAgo)
                    })
                    .ToListAsync();

                var psAggregatesDict = psAggregates.ToDictionary(a => a.ShelfId);


                // 2. Join Shelves with the calculated metrics (Runs in SQL and C#)
                var shelfMetricsQuery = await _dbContext.Shelves
                    .Select(s => new
                    {
                        Shelf = s,
                        // Attempt to retrieve pre-calculated data, default to null if no products are assigned
                        Metrics = psAggregatesDict.ContainsKey(s.ShelfId) ? psAggregatesDict[s.ShelfId] : null
                    })
                    .ToListAsync();

                // 3. Map and Calculate Final Metrics in C#
                var finalMetrics = shelfMetricsQuery.Select(s =>
                {
                    int currentStock = s.Metrics?.CurrentStock ?? 0;
                    int totalProducts = s.Metrics?.TotalProductsAssigned ?? 0;
                    // CHANGE: Use the 30-day count variable
                    int restockCount = s.Metrics?.RestockCountLast30Days ?? 0;
                    const int analysisDays = 30; // Define the analysis period for frequency calculation

                    var metric = new ShelfMetric
                    {
                        ShelfId = s.Shelf.ShelfId,
                        ShelfCode = s.Shelf.ShelfCode,
                        TotalCapacity = s.Shelf.Capacity,
                        CurrentStock = currentStock,
                        TotalProductsAssigned = totalProducts,

                        // CHANGE: Field name updated to reflect the 30-day window
                        RestockCountLast30Days = restockCount,

                        // Capacity Occupancy Calculation (No change to logic)
                        OccupancyPercentage = s.Shelf.Capacity > 0
                            ? Math.Round(((double)currentStock / s.Shelf.Capacity) * 100, 2)
                            : 0.0,

                        // Restocking Frequency Calculation
                        // CHANGE: Divide by 30 days 
                        AverageDaysBetweenRestocks = restockCount > 0
                            ? Math.Round((double)analysisDays / restockCount, 1)
                            : 0.0
                    };
                    return metric;
                }).ToList();

                return Ok(new
                {
                    message = "Shelf capacity and restocking metrics retrieved successfully (30-day analysis).",
                    data = finalMetrics
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error calculating shelf metrics.",
                    details = ex.Message
                });
            }
        }

        [Authorize(Roles = "admin,manager,staff")] // Accessible to both staff and managers
        [HttpGet("predict-depletion")]
        public async Task<IActionResult> PredictDepletion()
        {
            try
            {
                // Step 1: Calculate sales velocity (Average Daily Sales)
                var velocityData = await (
                    from sale in _dbContext.SalesHistories
                        // Group by ProductId AND the SaleTime's date component to get daily totals
                    group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
                    select new
                    {
                        daily.Key.ProductId,
                        DailySales = daily.Sum(x => x.Quantity)
                    }
                ).ToListAsync();

                // Calculate the average of the DailySales for each product
                var salesVelocity = velocityData
                    .GroupBy(x => x.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        SalesVelocity = g.Average(x => x.DailySales)
                    }).ToDictionary(x => x.ProductId, x => x.SalesVelocity);

                // Step 2: Get all ProductShelf entries (inventory)
                var shelves = await _dbContext.ProductShelves
                    .ToListAsync(); // No need to Include Shelf here

                var alertsToInsert = new List<ReplenishmentAlert>();
                var predictions = new List<StockDepletionPredictionDto>();
                var today = DateTime.Today;

                // Step 3: Loop through shelves and predict depletion
                foreach (var ps in shelves)
                {
                    try
                    {
                        // Only proceed if we have sales data for this product
                        if (!salesVelocity.ContainsKey(ps.ProductId)) continue;

                        var velocity = salesVelocity[ps.ProductId];

                        // Prediction Calculation
                        double daysToDepletion = double.MaxValue;
                        DateTime? expectedDate = null;
                        bool isLowStock = false;

                        if (velocity > 0)
                        {
                            daysToDepletion = Math.Round(ps.Quantity / velocity, 2);
                            expectedDate = today.AddDays(Math.Round(daysToDepletion));
                            isLowStock = daysToDepletion < 6; // Low stock threshold: less than 5 days supply
                        }

                        predictions.Add(new StockDepletionPredictionDto
                        {
                            ProductId = ps.ProductId,
                            ShelfId = ps.ShelfId,
                            Quantity = ps.Quantity,
                            SalesVelocity = velocity,
                            DaysToDepletion = daysToDepletion,
                            ExpectedDepletionDate = expectedDate,
                            IsLowStock = isLowStock
                        });

                        // Step 4: Generate Alert if Low Stock
                        if (isLowStock)
                        {
                            var urgency = daysToDepletion switch
                            {
                                <= 1 => "critical",
                                <= 2 => "high",
                                <= 4 => "medium",
                                _ => "low" 
                            };

                            // Check if an unfulfilled alert already exists to prevent duplicates
                            var exists = await _dbContext.ReplenishmentAlerts.AnyAsync(a =>
                                a.ProductId == ps.ProductId &&
                                a.ShelfId == ps.ShelfId);
                                //a.FulfillmentNote == null);

                            if (!exists)
                            {
                                alertsToInsert.Add(new ReplenishmentAlert
                                {
                                    ProductId = ps.ProductId,
                                    ShelfId = ps.ShelfId,
                                    PredictedDepletionDate = expectedDate ?? today,
                                    UrgencyLevel = urgency,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        // Handle cases where prediction fails for a specific product
                        // (e.g., if Quantity or Sales Velocity are invalid)
                    }
                }

                // Step 5: Save new alerts to the database
                if (alertsToInsert.Any())
                {
                    await _dbContext.ReplenishmentAlerts.AddRangeAsync(alertsToInsert);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Stock depletion predictions and alerts generated successfully.",
                    data = predictions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error generating stock depletion predictions.",
                    details = ex.Message
                });
            }
        }

        // <summary>
        /// Retrieves active (unfulfilled) low-stock replenishment alerts.
        /// </summary>
       
    }
}