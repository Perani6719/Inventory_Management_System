using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ShelfSense.WebAPi.Controllers;
using ShelfSense.Application.Interfaces;
using ShelfSense.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShelfSense.Tests
{
    public class AnalyticsControllerTests
    {
        private readonly Mock<IAnalyticsService> _analyticsServiceMock;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            _analyticsServiceMock = new Mock<IAnalyticsService>();
            _controller = new AnalyticsController(_analyticsServiceMock.Object);
        }

        [Fact]
        public async Task GetStockoutReport_ShouldReturnOk_WhenValidDates()
        {
            var startDate = new DateTime(2025, 10, 1);
            var endDate = new DateTime(2025, 10, 10);
            var adjustedEndDate = endDate.AddDays(1);

            var mockReport = new List<StockoutReportItem>
            {
                new StockoutReportItem
                {
                    ProductId = 1,
                    ShelfId = 101,
                    ProductName = "Test Product",
                    ShelfLocation = "Aisle 1",
                    StockoutCount = 3,
                    AvgReplenishmentTimeInHours = 1.5,
                    AvgReplenishmentDelayInHours = 0.5,
                    ShelfAvailabilityPercentage = 97.5
                }
            };

            _analyticsServiceMock
                .Setup(s => s.GenerateStockoutReportAsync(startDate.Date, adjustedEndDate.Date, endDate.Date))
                .ReturnsAsync(mockReport);

            var result = await _controller.GetStockoutReport(startDate, endDate);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsAssignableFrom<IEnumerable<StockoutReportItem>>(okResult.Value);
            Assert.Single(returnedReport);
        }

        [Fact]
        public async Task GetStockoutReport_ShouldReturnBadRequest_WhenStartDateAfterEndDate()
        {
            var startDate = new DateTime(2025, 10, 10);
            var endDate = new DateTime(2025, 10, 1);

            var result = await _controller.GetStockoutReport(startDate, endDate);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Start date must be before end date.", badRequest.Value);
        }

        [Fact]
        public async Task GetStockoutReportPdf_ShouldReturnFile_WhenValidDates()
        {
            var startDate = new DateTime(2025, 10, 1);
            var endDate = new DateTime(2025, 10, 10);
            var adjustedEndDate = endDate.AddDays(1);

            var fakePdf = new byte[] { 1, 2, 3, 4 };

            _analyticsServiceMock
                .Setup(s => s.GenerateStockoutReportPdfAsync(startDate.Date, adjustedEndDate.Date, endDate.Date))
                .ReturnsAsync(fakePdf);

            var result = await _controller.GetStockoutReportPdf(startDate, endDate);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal(fakePdf, fileResult.FileContents);
        }

        [Fact]
        public async Task GetStockoutReportPdf_ShouldReturnBadRequest_WhenStartDateAfterEndDate()
        {
            var startDate = new DateTime(2025, 10, 10);
            var endDate = new DateTime(2025, 10, 1);

            var result = await _controller.GetStockoutReportPdf(startDate, endDate);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Start date must be before end date.", badRequest.Value);
        }
    }
}
