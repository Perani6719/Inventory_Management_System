using ShelfSense.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    public interface IAnalyticsService
    {
        /// <summary>
        /// Generates a report on stockouts and replenishment efficiency for a given period.
        /// </summary>
        /// <param name="startDate">Start date for the report period.</param>
        /// <param name="adjustedEndDate">End date used for querying (exclusive upper bound).</param>
        /// <param name="originalEndDate">Original end date used for display in the report.</param>
        /// <returns>A list of stockout report items.</returns>
        Task<IEnumerable<StockoutReportItem>> GenerateStockoutReportAsync(DateTime startDate, DateTime adjustedEndDate, DateTime originalEndDate);

        /// <summary>
        /// Generates a PDF report on stockouts and replenishment efficiency for a given period.
        /// </summary>
        /// <param name="startDate">Start date for the report period.</param>
        /// <param name="adjustedEndDate">End date used for querying (exclusive upper bound).</param>
        /// <param name="originalEndDate">Original end date used for display in the report.</param>
        /// <returns>A PDF file as a byte array.</returns>
        Task<byte[]> GenerateStockoutReportPdfAsync(DateTime startDate, DateTime adjustedEndDate, DateTime originalEndDate);
    }
}
