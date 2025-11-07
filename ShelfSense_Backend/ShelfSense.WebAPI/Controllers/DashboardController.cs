//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using ShelfSense.Application.DTOs;
//using ShelfSense.Application.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace ShelfSense.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize] // Requires authentication, perhaps Role="manager" or "staff"
//    public class DashboardController : ControllerBase
//    {
//        private readonly IDashboardService _dashboardService;
//        private readonly ILogger<DashboardController> _logger;

//        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
//        {
//            _dashboardService = dashboardService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Retrieves the real-time inventory status summary for the dashboard.
//        /// </summary>
//        [HttpGet("inventory-summary")]
//        public async Task<IActionResult> GetInventorySummary()
//        {
//            try
//            {
//                // This call is usually authorized to check the user's store_id via claims
//                _logger.LogInformation("Dashboard inventory summary requested.");

//                // Delegate the complex logic to the service layer
//                List<DashboardInventoryReportResponse> responseData =
//                    await _dashboardService.GetInventorySummaryAsync();

//                _logger.LogInformation("Successfully retrieved {Count} inventory summary items.", responseData.Count);

//                return Ok(new
//                {
//                    message = "Dashboard inventory summary retrieved successfully.",
//                    data = responseData
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating dashboard summary.");

//                return StatusCode(500, new
//                {
//                    message = "Error generating dashboard summary.",
//                    details = ex.Message
//                });
//            }
//        }
//    }
//}