//using AutoMapper;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging; // ADDED
//using ShelfSense.Application.DTOs;
//using ShelfSense.Application.Interfaces;
//using ShelfSense.Domain.Entities;
//using ShelfSense.Infrastructure.Services;
//using static ShelfSense.Application.DTOs.ProductDto;

//namespace ShelfSense.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ProductController : ControllerBase
//    {
//        private readonly IProductRepository _repository;
//        private readonly IMapper _mapper;
//        private readonly ILogger<ProductController> _logger; // ADDED: Private readonly field for ILogger
//        private readonly BlobStorageService _blobService;
//        public ProductController(IProductRepository repository, IMapper mapper, ILogger<ProductController> logger, BlobStorageService blobService) // ADDED: ILogger to the constructor
//        {
//            _repository = repository;
//            _mapper = mapper;
//            _logger = logger; // ADDED: Assignment in the constructor
//            _blobService = blobService;
//        }

//        // 🔓 Accessible to any authenticated user
//        [Authorize(Roles = "admin,manager,staff")]
//        [HttpGet]
//        public IActionResult GetAllProducts()
//        {
//            _logger.LogInformation("User {User} attempting to retrieve all products.", User.Identity?.Name); // LOGGING START
//            try
//            {
//                var products = _repository.GetAllProducts().ToList();
//                var response = _mapper.Map<List<ProductResponse>>(products);
//                _logger.LogInformation("Successfully retrieved {Count} products.", response.Count); // LOGGING SUCCESS
//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while retrieving all products."); // LOGGING ERROR
//                return StatusCode(500, new { message = "An error occurred while retrieving products.", detail = ex.Message });
//            }
//        }

//        // 🔓 Accessible to any authenticated user
//        [Authorize(Roles = "admin,manager,staff")]
//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetProductById(long id)
//        {
//            _logger.LogInformation("User {User} attempting to retrieve product with ID: {ProductId}.", User.Identity?.Name, id); // LOGGING START
//            try
//            {
//                var product = await _repository.GetProductByIdAsync(id);
//                if (product == null)
//                {
//                    _logger.LogWarning("Product not found with ID: {ProductId}.", id); // LOGGING NOT FOUND
//                    return NotFound(new { message = $"Product with ID {id} not found." });
//                }

//                var response = _mapper.Map<ProductResponse>(product);
//                _logger.LogInformation("Successfully retrieved product: {ProductName} (ID: {ProductId}).", response.ProductName, id); // LOGGING SUCCESS
//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while retrieving product ID: {ProductId}.", id); // LOGGING ERROR
//                return StatusCode(500, new { message = $"An error occurred while retrieving product {id}.", detail = ex.Message });
//            }
//        }

//        // 🔐 Restricted to manager role
//        [Authorize(Roles = "admin,manager,staff")]
//        [HttpPost]
//        public async Task<IActionResult> CreateProdcuts([FromBody] ProductCreateRequest request)
//        {
//            _logger.LogInformation("Manager {Manager} attempting to create product: {ProductName} (SKU: {SKU})", User.Identity?.Name, request?.ProductName, request?.StockKeepingUnit); // LOGGING START
//            try
//            {
//                if (request == null)
//                {
//                    _logger.LogWarning("Product creation failed: Request body was null."); // LOGGING BAD REQUEST
//                    return BadRequest(new { message = "Request body cannot be null." });
//                }

//                if (!ModelState.IsValid)
//                {
//                    _logger.LogWarning("Product creation failed: Invalid model state for product {ProductName}.", request.ProductName); // LOGGING BAD REQUEST
//                    return BadRequest(ModelState);
//                }

//                var product = _mapper.Map<Product>(request);

//                try
//                {
//                    await _repository.AddProductAsync(product);
//                }
//                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
//                {
//                    _logger.LogWarning(ex, "Product creation failed due to duplicate SKU: {SKU}", request.StockKeepingUnit); // LOGGING CONFLICT
//                    return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
//                }

//                var response = _mapper.Map<ProductResponse>(product);

//                _logger.LogInformation("Product created successfully: {ProductName} (ID: {ProductId})", response.ProductName, response.ProductId); // LOGGING SUCCESS

//                // NOTE: The automatic assignment logic is MOVED to ProductShelfController.
//                // The client (frontend/other service) is now responsible for making a follow-up 
//                // call to the new api/ProductShelf/auto-assign endpoint.

//                return CreatedAtAction(nameof(GetProductById), new { id = response.ProductId }, new
//                {
//                    message = $"Product '{response.ProductName}' created successfully. Please call the ProductShelf/auto-assign endpoint to link it to a shelf.",
//                    data = response
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An unexpected error occurred while creating product: {ProductName} (SKU: {SKU})", request?.ProductName, request?.StockKeepingUnit); // LOGGING ERROR
//                return StatusCode(500, new { message = "An error occurred while creating the product.", detail = ex.Message });
//            }
//        }

//        // 🔐 Restricted to manager role
//        [Authorize(Roles = "admin,manager,staff")]
//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateProductDetails(long id, [FromBody] ProductCreateRequest request)
//        {
//            _logger.LogInformation("Manager {Manager} attempting to update product ID: {ProductId}.", User.Identity?.Name, id); // LOGGING START
//            try
//            {
//                if (request == null)
//                {
//                    _logger.LogWarning("Product update failed for ID {ProductId}: Request body was null.", id); // LOGGING BAD REQUEST
//                    return BadRequest(new { message = "Request body cannot be null." });
//                }

//                if (!ModelState.IsValid)
//                {
//                    _logger.LogWarning("Product update failed for ID {ProductId}: Invalid model state.", id); // LOGGING BAD REQUEST
//                    return BadRequest(ModelState);
//                }

//                var existing = await _repository.GetProductByIdAsync(id);
//                if (existing == null)
//                {
//                    _logger.LogWarning("Product update failed: Product ID {ProductId} not found.", id); // LOGGING NOT FOUND
//                    return NotFound(new { message = $"Product with ID {id} not found." });
//                }

//                string oldSKU = existing.StockKeepingUnit;
//                string oldName = existing.ProductName;

//                existing.StockKeepingUnit = request.StockKeepingUnit;
//                existing.ProductName = request.ProductName;
//                existing.CategoryId = request.CategoryId;
//                existing.PackageSize = request.PackageSize;
//                existing.Unit = request.Unit;

//                try
//                {
//                    await _repository.UpdateProductAsync(existing);
//                    _logger.LogInformation("Product ID {ProductId} updated successfully. Name: '{OldName}' -> '{NewName}', SKU: '{OldSKU}' -> '{NewSKU}'.",
//                        id, oldName, existing.ProductName, oldSKU, existing.StockKeepingUnit); // LOGGING SUCCESS
//                }
//                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
//                {
//                    _logger.LogWarning(ex, "Product update failed for ID {ProductId} due to duplicate SKU: {SKU}", id, request.StockKeepingUnit); // LOGGING CONFLICT
//                    return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
//                }

//                return Ok(new { message = $"Product with ID {id} updated successfully." });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An unexpected error occurred while updating product ID: {ProductId}.", id); // LOGGING ERROR
//                return StatusCode(500, new { message = $"An error occurred while updating product {id}.", detail = ex.Message });
//            }
//        }

//        // 🔐 Restricted to manager role
//        [Authorize(Roles = "admin,manager,staff")]
//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteProduct(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
//        {
//            _logger.LogInformation("Manager {Manager} attempting to delete product ID: {ProductId}. Confirmation: {Confirmation}", User.Identity?.Name, id, confirm); // LOGGING START
//            try
//            {
//                if (!confirm)
//                {
//                    _logger.LogWarning("Product deletion for ID {ProductId} rejected: Deletion not confirmed.", id); // LOGGING BAD REQUEST
//                    return BadRequest(new
//                    {
//                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
//                    });
//                }

//                var existing = await _repository.GetProductByIdAsync(id);
//                if (existing == null)
//                {
//                    _logger.LogWarning("Product deletion failed: Product ID {ProductId} not found.", id); // LOGGING NOT FOUND
//                    return NotFound(new { message = $"Product with ID {id} not found." });
//                }

//                try
//                {
//                    await _repository.DeleteProductAsync(id);
//                    _logger.LogInformation("Product ID {ProductId} deleted successfully.", id); // LOGGING SUCCESS
//                }
//                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
//                {
//                    _logger.LogWarning(ex, "Product deletion failed for ID {ProductId} due to foreign key constraint violation.", id); // LOGGING CONFLICT
//                    return Conflict(new
//                    {
//                        message = $"Cannot delete Product ID {id} because it is referenced in other records (e.g., RestockTask or ReplenishmentAlert)."
//                    });
//                }

//                return Ok(new { message = $"Product with ID {id} deleted successfully." });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An unexpected error occurred while deleting product ID: {ProductId}.", id); // LOGGING ERROR
//                return StatusCode(500, new { message = $"An error occurred while deleting product {id}.", detail = ex.Message });
//            }
//        }

//    }
//}



using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Services;
using static ShelfSense.Application.DTOs.ProductDto;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductController> _logger;
        private readonly BlobStorageService _blobService;

        public ProductController(
            IProductRepository repository,
            IMapper mapper,
            ILogger<ProductController> logger,
            BlobStorageService blobService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Attempting to retrieve all products.");
            try
            {
                var products = _repository.GetAllProducts().ToList();
                var response = _mapper.Map<List<ProductResponse>>(products);
                _logger.LogInformation("Successfully retrieved {Count} products.", response.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving products.");
                return StatusCode(500, new { message = "An error occurred while retrieving products.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogInformation("Attempting to retrieve product with ID: {ProductId}.", id);
            try
            {
                var product = await _repository.GetProductByIdAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found with ID: {ProductId}.", id);
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var response = _mapper.Map<ProductResponse>(product);
                _logger.LogInformation("Successfully retrieved product: {ProductName} (ID: {ProductId}).", response.ProductName, id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving product ID: {ProductId}.", id);
                return StatusCode(500, new { message = $"An error occurred while retrieving product {id}.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ProductCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to create a new product: {ProductName} (SKU: {SKU}) by user {User}", request?.ProductName, request?.StockKeepingUnit, User.Identity?.Name);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Product creation failed: Request body was null.");
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Product creation failed: Invalid model state for product {ProductName}.", request.ProductName);
                    return BadRequest(ModelState);
                }

                string? imageUrl = null;
                if (image != null)
                {
                    try
                    {
                        imageUrl = await _blobService.UploadImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Image upload failed for product: {ProductName} (SKU: {SKU})", request.ProductName, request.StockKeepingUnit);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                var product = _mapper.Map<Product>(request);
                product.ImageUrl = imageUrl;

                try
                {
                    await _repository.AddProductAsync(product);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
                {
                    _logger.LogWarning(ex, "Product creation failed due to duplicate SKU: {SKU}", request.StockKeepingUnit);
                    return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
                }

                var response = _mapper.Map<ProductResponse>(product);
                _logger.LogInformation("Product created successfully: {ProductName} (ID: {ProductId})", response.ProductName, response.ProductId);

                return CreatedAtAction(nameof(GetById), new { id = response.ProductId }, new
                {
                    message = $"Product '{response.ProductName}' created successfully. Please call the ProductShelf/auto-assign endpoint to link it to a shelf.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating product: {ProductName} (SKU: {SKU})", request?.ProductName, request?.StockKeepingUnit);
                return StatusCode(500, new { message = "An error occurred while creating the product.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromForm] ProductCreateRequest request, IFormFile? image)
        {
            _logger.LogInformation("Attempting to update product ID: {ProductId} to name: {NewName}", id, request?.ProductName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Product update failed for ID {ProductId}: Request body was null.", id);
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Product update failed for ID {ProductId}: Invalid model state.", id);
                    return BadRequest(ModelState);
                }

                var existing = await _repository.GetProductByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Product update failed: Product ID {ProductId} not found.", id);
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                string oldName = existing.ProductName;
                string oldSKU = existing.StockKeepingUnit;

                existing.StockKeepingUnit = request.StockKeepingUnit;
                existing.ProductName = request.ProductName;
                existing.CategoryId = request.CategoryId;
                existing.PackageSize = request.PackageSize;
                existing.Unit = request.Unit;

                if (image != null)
                {
                    try
                    {
                        existing.ImageUrl = await _blobService.UploadImageAsync(image);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Image upload failed during update for product ID: {ProductId}", id);
                        return StatusCode(500, new { message = "Image upload failed.", detail = ex.Message });
                    }
                }

                try
                {
                    await _repository.UpdateProductAsync(existing);
                    _logger.LogInformation("Product ID {ProductId} updated successfully. Name: '{OldName}' → '{NewName}', SKU: '{OldSKU}' → '{NewSKU}'.",
                        id, oldName, existing.ProductName, oldSKU, existing.StockKeepingUnit);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Products_StockKeepingUnit") == true)
                {
                    _logger.LogWarning(ex, "Product update failed for ID {ProductId} due to duplicate SKU: {SKU}", id, request.StockKeepingUnit);
                    return Conflict(new { message = $"SKU '{request.StockKeepingUnit}' already exists." });
                }

                return Ok(new { message = $"Product with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating product ID: {ProductId}.", id);
                return StatusCode(500, new { message = $"An error occurred while updating product {id}.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "admin,manager,staff")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Confirm-Delete")] bool confirm)
        {
            _logger.LogInformation("Attempting to delete product ID: {ProductId}. Confirmation: {Confirmation}", id, confirm);

            try
            {
                if (!confirm)
                {
                    _logger.LogWarning("Product deletion for ID {ProductId} rejected: Deletion not confirmed.", id);
                    return BadRequest(new
                    {
                        message = "Deletion not confirmed. Please set 'X-Confirm-Delete: true' in the request header to proceed."
                    });
                }

                var existing = await _repository.GetProductByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Product deletion failed: Product ID {ProductId} not found.", id);
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                try
                {
                    await _repository.DeleteProductAsync(id);
                    _logger.LogInformation("Product ID {ProductId} deleted successfully.", id);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true)
                {
                    _logger.LogWarning(ex, "Product deletion failed for ID {ProductId} due to foreign key constraint violation.", id);
                    return Conflict(new
                    {
                        message = $"Cannot delete Product ID {id} because it is referenced in other records (e.g., RestockTask or ReplenishmentAlert)."
                    });
                }

                return Ok(new { message = $"Product with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting product ID: {ProductId}.", id);
                return StatusCode(500, new { message = $"An error occurred while deleting product {id}.", detail = ex.Message });
            }
        }
    }
}
