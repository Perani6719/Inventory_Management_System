using ShelfSense.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShelfSense.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for retrieving dashboard-specific inventory summary data.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Retrieves the current inventory status for all product-shelf combinations.
        /// </summary>
        /// <returns>A list of dashboard inventory report responses.</returns>
        Task<List<DashboardInventoryReportResponse>> GetInventorySummaryAsync();
    }
}