using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelfSense.Application.Interfaces;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliveryStatusLogController : ControllerBase
    {
        private readonly IDeliveryStatusLog _repository;
        private readonly ILogger<DeliveryStatusLogController> _logger;

        public DeliveryStatusLogController(IDeliveryStatusLog repository, ILogger<DeliveryStatusLogController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,manager,staff")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Retrieving all delivery status logs.");
            var logs = await _repository.GetAllAsync();
            return Ok(new { message = "Delivery status logs retrieved.", data = logs });
        }
    }

}
