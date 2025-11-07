using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Domain.Identity;
using ShelfSense.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IStaffRepository _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IStaffRepository repository,
            IMapper mapper,
            ShelfSenseDbContext context,
            ILogger<AdminController> logger,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpPost("manager")]
        public async Task<IActionResult> CreateManager([FromBody] ManagerCreateRequest request)
        {
            _logger.LogInformation("Admin attempting to create new Manager: {Email} for Store ID: {StoreId}.",
                                   request?.Email, request?.StoreId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. Check if email already exists in Identity
                if (await _userManager.FindByEmailAsync(request.Email) != null)
                {
                    return Conflict(new { message = $"Email '{request.Email}' is already registered." });
                }

                // 2. Check if store exists
                var storeExists = await _context.Set<Store>().AnyAsync(s => s.StoreId == request.StoreId);
                if (!storeExists)
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });

                // 3. Check if manager already exists for this store
                var existingManager = await _repository.GetAll()
                    .AnyAsync(s => s.StoreId == request.StoreId && s.Role == "manager");
                if (existingManager)
                    return Conflict(new { message = $"A manager already exists for Store ID '{request.StoreId}'." });

                // 4. Create Identity user
                var appUser = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    EmailConfirmed = true
                };

                var identityResult = await _userManager.CreateAsync(appUser, request.PasswordHash);
                if (!identityResult.Succeeded)
                {
                    var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                    _logger.LogError("Identity user creation failed for {Email}: {Errors}", request.Email, errors);
                    return BadRequest(new { message = "Failed to create user credentials.", details = errors });
                }

                // 5. Assign role
                await _userManager.AddToRoleAsync(appUser, "manager");

                // 6. Create Staff entity
                var entity = _mapper.Map<Staff>(request);
                entity.Role = "manager";
                entity.CreatedAt = DateTime.UtcNow;
                entity.PasswordHash = appUser.PasswordHash;

                await _repository.AddAsync(entity);

                // 7. Send welcome email via Hangfire
                try
                {
                    string subject = "Store Manager Account Created";
                    string body = $"<p>Hello <strong>{entity.Name}</strong>, your new manager account for Store {entity.StoreId} has been created. Your initial password is: <strong>{request.PasswordHash}</strong>.</p>";

                    BackgroundJob.Enqueue(() =>
                        _emailService.SendEmailAsync(entity.Email, subject, body)
                    );

                    _logger.LogInformation("Manager welcome email job for {Email} successfully enqueued to Hangfire.", entity.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Warning: Failed to enqueue email job to new manager {Email}.", entity.Email);
                }

                var response = _mapper.Map<StaffResponse>(entity);
                return CreatedAtAction("GetById", "Staff", new { id = response.StaffId }, new
                {
                    message = "Manager record created successfully. Welcome email is being processed in the background.",
                    data = response
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Staff_Email") == true)
            {
                return Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Manager record for {Email}.", request?.Email);
                return StatusCode(500, new { message = "An error occurred while creating the manager record.", details = ex.Message });
            }
        }
    }
}
