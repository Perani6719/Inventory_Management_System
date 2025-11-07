using Microsoft.AspNetCore.Mvc;
using ShelfSense.Application.Interfaces;
using System;
using System.Threading.Tasks;
using ShelfSense.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace ShelfSense.WebAPi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Generates the Inventory Stockout and Efficiency Report in JSON format.
        /// </summary>
        /// <param name="startDate">Start date for the report period (e.g., 2023-01-01).</param>
        /// <param name="endDate">End date for the report period (e.g., 2023-01-31).</param>
        /// <returns>A list of stockout and efficiency metrics per product/shelf.</returns>
        /// 
        [HttpGet("stockout-report")]
        [Produces("application/json")]
        [Authorize(Roles="manager,staff")]
        public async Task<IActionResult> GetStockoutReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate >= endDate)
                return BadRequest("Start date must be before end date.");

            if (endDate.Date > DateTime.Today)
                return BadRequest("End date cannot be in the future.");

            try
            {
                var adjustedEndDate = endDate.Date.AddDays(1); // Include full endDate
                var report = await _analyticsService.GenerateStockoutReportAsync(startDate.Date, adjustedEndDate, endDate.Date);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates the Inventory Stockout and Efficiency Report as a downloadable PDF.
        /// </summary>
        /// <param name="startDate">Start date for the report period (e.g., 2023-01-01).</param>
        /// <param name="endDate">End date for the report period (e.g., 2023-01-31).</param>
        /// <returns>A PDF file containing the stockout and efficiency metrics.</returns>
        [HttpGet("stockout-report/pdf")]
        [Authorize(Roles = "admin,manager,staff")]
        public async Task<IActionResult> GetStockoutReportPdf(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate >= endDate)
                return BadRequest("Start date must be before end date.");

            if (endDate.Date > DateTime.Today)
                return BadRequest("End date cannot be in the future.");

            try
            {
                var adjustedEndDate = endDate.Date.AddDays(1); // Include full endDate
                var pdfBytes = await _analyticsService.GenerateStockoutReportPdfAsync(startDate.Date, adjustedEndDate, endDate.Date);
                return File(pdfBytes, "application/pdf", $"StockoutReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
