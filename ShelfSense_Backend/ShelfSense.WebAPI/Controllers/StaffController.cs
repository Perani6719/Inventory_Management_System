using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShelfSense.Application.DTOs;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using ShelfSense.Domain.Identity; // Assuming ApplicationUser is here

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repository;
        private readonly IMapper _mapper;
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<StaffController> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(
            IStaffRepository repository,
            IMapper mapper,
            ShelfSenseDbContext context,
            ILogger<StaffController> logger,
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

        // 🔓 GET all (Accessible to manager and staff)
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var staff = _repository.GetAll().ToList();
                var response = _mapper.Map<List<StaffResponse>>(staff);
                return Ok(new { message = "Staff records retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all staff records.");
                return StatusCode(500, new { message = "An error occurred while retrieving staff records.", details = ex.Message });
            }
        }

        // 🔓 GET by ID (Accessible to manager and staff)
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var staff = await _repository.GetByIdAsync(id);
                if (staff == null)
                {
                    return NotFound(new { message = $"Staff ID {id} not found." });
                }

                var response = _mapper.Map<StaffResponse>(staff);
                return Ok(new { message = "Staff record retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving staff record ID {StaffId}.", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the staff record.", details = ex.Message });
            }
        }

        // 🔐 POST (CREATE) - Identity and Staff creation
        //[Authorize(Roles = "admin,manager")]
        //[Authorize(Roles = "admin,manager")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffCreateRequest request)
        {
            _logger.LogInformation("Attempting to create staff: {Email}.", request?.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. Check if the email is already registered in Identity
                if (await _userManager.FindByEmailAsync(request.Email) != null)
                {
                    return Conflict(new { message = $"Email '{request.Email}' is already registered." });
                }

                // 2. Foreign Key Check (Store Exists)
                var storeExists = await _context.Set<Store>().AnyAsync(s => s.StoreId == request.StoreId);
                if (!storeExists)
                {
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                // 3. Create the ASP.NET Identity User (Handles Hashing)
                var appUser = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    EmailConfirmed = true,
                };

                var identityResult = await _userManager.CreateAsync(appUser, request.PasswordHash);

                if (!identityResult.Succeeded)
                {
                    var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                    _logger.LogError("Identity user creation failed for {Email}: {Errors}", request.Email, errors);
                    return BadRequest(new { message = "Failed to create user credentials.", details = errors });
                }

                // 4. Assign the Role in Identity
                await _userManager.AddToRoleAsync(appUser, request.Role);

                // 5. Create and persist the custom Staff entity (for business data)
                var staffEntity = _mapper.Map<Staff>(request);
                staffEntity.CreatedAt = DateTime.UtcNow;

                // FIX: Access the PasswordHash property directly from the ApplicationUser object
                staffEntity.PasswordHash = appUser.PasswordHash;

                // 6. Persist Custom Data
                await _repository.AddAsync(staffEntity);

                // 7. Send Confirmation Email (Offload to Hangfire)
                try
                {
                    string subject = "Welcome to ShelfSense Staff!";
                    string body = $"<p>Hello <strong>{staffEntity.Name}</strong>,...</p> You are now a member in my store. Please log in to enjoy our services!!!";

                    BackgroundJob.Enqueue(() =>
                        _emailService.SendEmailAsync(staffEntity.Email, subject, body)
                    );

                    _logger.LogInformation("Welcome email job for {Email} successfully enqueued to Hangfire.", staffEntity.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Warning: Failed to enqueue welcome email job for {Email}.", staffEntity.Email);
                }

                var response = _mapper.Map<StaffResponse>(staffEntity);

                // 🌟 FIX: Use the correct method name: GetById
                return CreatedAtAction(nameof(GetById), new { id = response.StaffId }, new
                {
                    message = "Staff record created successfully. Welcome email is being processed in the background.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating staff record for {Email}.", request?.Email);
                return StatusCode(500, new { message = "An error occurred while creating the staff record.", details = ex.Message });
            }
        }

        // 🔐 PUT (REPLACE) - Identity and Staff update
        [Authorize(Roles = "admin,manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] StaffCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingStaff = await _repository.GetByIdAsync(id);
                if (existingStaff == null)
                {
                    return NotFound(new { message = $"Staff ID {id} not found." });
                }

                var storeExists = await _context.Set<Store>().AnyAsync(s => s.StoreId == request.StoreId);
                if (!storeExists)
                {
                    return BadRequest(new { message = $"Store ID '{request.StoreId}' does not exist." });
                }

                // 1. Find the corresponding Identity user
                var identityUser = await _userManager.FindByEmailAsync(existingStaff.Email);

                if (identityUser == null)
                {
                    return StatusCode(500, new { message = "Cannot update credentials: Linked Identity user not found." });
                }

                // 2. Update Identity Credentials and Role
                // Update Email (if changed)
                if (request.Email != existingStaff.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(identityUser, request.Email);
                    var setUserNameResult = await _userManager.SetUserNameAsync(identityUser, request.Email);
                    if (!setEmailResult.Succeeded || !setUserNameResult.Succeeded)
                    {
                        return BadRequest(new { message = "Failed to update Identity email/username." });
                    }
                }

                // Update Role
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                if (!currentRoles.Contains(request.Role))
                {
                    await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                    await _userManager.AddToRoleAsync(identityUser, request.Role);
                }

                // Update PasswordHash
                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var resetResult = await _userManager.ResetPasswordAsync(identityUser, token, request.PasswordHash);

                if (!resetResult.Succeeded)
                {
                    var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = "Failed to update Identity password.", details = errors });
                }

                // 3. Update custom Staff entity properties
                existingStaff.StoreId = request.StoreId;
                existingStaff.Name = request.Name;
                existingStaff.Role = request.Role;
                existingStaff.Email = request.Email;

                // FIX: Save Identity user to update the PasswordHash property on the object, then copy
                await _userManager.UpdateAsync(identityUser);
                existingStaff.PasswordHash = identityUser.PasswordHash;

                await _repository.UpdateAsync(existingStaff);

                return Ok(new { message = $"Staff record ID {id} updated successfully." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Staff_Email") == true)
            {
                return Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating staff record ID {StaffId}.", id);
                return StatusCode(500, new { message = "An error occurred while updating the staff record.", details = ex.Message });
            }
        }

        // 🔐 PATCH (PARTIAL UPDATE) - Identity and Staff partial update
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(long id, [FromBody] StaffUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Staff ID {id} not found." });
                }

                var identityUser = await _userManager.FindByEmailAsync(existing.Email);
                if (identityUser == null)
                {
                    _logger.LogError("Identity user not found for Staff ID {StaffId} with email {Email}.", id, existing.Email);
                    return StatusCode(500, new { message = "Associated user credentials not found in Identity system." });
                }

                // --- Apply Partial Updates ---

                if (request.StoreId.HasValue)
                {
                    var storeExists = await _context.Set<Store>().AnyAsync(s => s.StoreId == request.StoreId.Value);
                    if (!storeExists)
                    {
                        return BadRequest(new { message = $"Store ID '{request.StoreId.Value}' does not exist." });
                    }
                    existing.StoreId = request.StoreId.Value;
                }

                if (!string.IsNullOrEmpty(request.Name))
                {
                    existing.Name = request.Name;
                }

                if (!string.IsNullOrEmpty(request.Role))
                {
                    existing.Role = request.Role;
                    // Update Role in Identity
                    var currentRoles = await _userManager.GetRolesAsync(identityUser);
                    if (!currentRoles.Contains(request.Role))
                    {
                        await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                        await _userManager.AddToRoleAsync(identityUser, request.Role);
                    }
                }

                if (!string.IsNullOrEmpty(request.Email) && request.Email != existing.Email)
                {
                    // Update Email in Identity
                    var setEmailResult = await _userManager.SetEmailAsync(identityUser, request.Email);
                    var setUserNameResult = await _userManager.SetUserNameAsync(identityUser, request.Email);

                    if (!setEmailResult.Succeeded || !setUserNameResult.Succeeded)
                    {
                        var errors = setEmailResult.Errors.Concat(setUserNameResult.Errors).Select(e => e.Description);
                        return BadRequest(new
                        {
                            message = "Failed to update email/username in Identity.",
                            details = errors
                        });
                    }

                    // Update Staff entity only after successful Identity update
                    existing.Email = request.Email;
                }

                if (!string.IsNullOrEmpty(request.PasswordHash))
                {
                    // Update PasswordHash in Identity
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    var resetResult = await _userManager.ResetPasswordAsync(identityUser, token, request.PasswordHash);

                    if (!resetResult.Succeeded)
                    {
                        // 🚀 FIX: Return detailed Identity validation errors
                        var errors = resetResult.Errors.Select(e => e.Description);
                        return BadRequest(new
                        {
                            message = "Password update failed due to complexity or validation rules.",
                            details = errors
                        });
                    }

                    // Update Staff entity only after successful Identity update
                    // This ensures the hash is saved to the Identity DB and then copied to the Staff entity
                    await _userManager.UpdateAsync(identityUser);
                    existing.PasswordHash = identityUser.PasswordHash;
                }
                // --- End Partial Updates ---

                await _repository.UpdateAsync(existing);
                return Ok(new { message = $"Staff record ID {id} partially updated successfully." });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Staff_Email") == true)
            {
                return Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while partially updating staff record ID {StaffId}.", id);
                return StatusCode(500, new { message = "An error occurred while partially updating the staff record.", details = ex.Message });
            }
        }
        // 🔐 DELETE (Manager-only with confirmation)
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm = false)
        {
            if (!confirm)
            {
                return BadRequest(new
                {
                    message = "Deletion not confirmed. Please add header 'X-Confirm-Delete: true' to proceed."
                });
            }

            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Staff ID {id} not found." });
                }

                // 1. Delete the Identity User first
                var identityUser = await _userManager.FindByEmailAsync(existing.Email);
                if (identityUser != null)
                {
                    var deleteResult = await _userManager.DeleteAsync(identityUser);
                    if (!deleteResult.Succeeded)
                    {
                        _logger.LogError("Failed to delete Identity user for Staff ID {StaffId}.", id);
                        return StatusCode(500, new { message = "Failed to delete associated user credentials." });
                    }
                }

                // 2. Delete the Staff record
                await _repository.DeleteAsync(id);

                return Ok(new { message = $"Staff record ID {id} and associated credentials deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting staff record ID {StaffId}.", id);
                return StatusCode(500, new { message = "An error occurred while deleting the staff record.", details = ex.Message });
            }
        }
    }
}