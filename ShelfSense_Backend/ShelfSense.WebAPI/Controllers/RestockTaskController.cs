

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ShelfSense.Application.Interfaces;

using ShelfSense.Domain.Entities;

namespace ShelfSense.Api.Controllers

{

    [ApiController]

    [Route("api/[controller]")]

    public class RestockTaskController : ControllerBase

    {

        private readonly IRestockTaskRepository _restockTaskRepository;


        public RestockTaskController(IRestockTaskRepository restockTaskRepository)

        {

            _restockTaskRepository = restockTaskRepository;


        }

        // GET: api/restocktask
        [Authorize(Roles = "admin,manager,staff")]

        [HttpGet]

        public async Task<ActionResult<List<RestockTask>>> GetAllTasks()

        {

            var tasks = await _restockTaskRepository.GetAllAsync();

            return Ok(tasks);

        }
        [Authorize(Roles = "admin,manager,staff")]
        // GET: api/restocktask/staff/{staffId}

        [HttpGet("staff/{staffId}")]

        public async Task<ActionResult<List<RestockTask>>> GetTasksByStaff(long staffId)

        {

            var tasks = await _restockTaskRepository.GetByStaffIdAsync(staffId);

            return Ok(tasks);

        }
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("delayed")]

        public async Task<ActionResult> GetDelayedTasks()

        {

            var tasks = await _restockTaskRepository.GetDelayedTasksAsync();

            if (!tasks.Any())

                return Ok(new { message = "No delayed tasks are present now." });

            return Ok(new { message = $"Delayed tasks count: {tasks.Count}", tasks });

        }


        // POST: api/restocktask/assign-from-delivered
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost("assign-tasks")]
        public async Task<IActionResult> AssignTasks()
        {
            var resultMessage = await _restockTaskRepository.AssignTasksFromDeliveredStockAsync();

            return Ok(new { message = resultMessage });
        }

        //organize the products

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost("organize-product")]

        public async Task<IActionResult> OrganizeProduct(long staffId, long taskId)

        {

            var result = await _restockTaskRepository.OrganizeDeliveredProductAsync(taskId, staffId);

            return Ok(result);

        }


        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost("check-status/{taskId}")]

        public async Task<IActionResult> CheckStatus(long taskId)

        {

            var message = await _restockTaskRepository.CheckStatusByIdAsync(taskId);

            if (message == null)

                return BadRequest("Unable to check status. Task may not be completed yet or task not found.");

            return Ok(message);

        }

    }

}