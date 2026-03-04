using HomeLabManager.API.Services;
using HomeLabManager.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HomeLabManager.API.Controllers
{
    // This controller is responsible for handling requests related to components, such as creating, updating, and deleting components. It will also handle requests related to the relationship between devices and components, such as adding a component to a device or removing a component from a device.
    [ApiController]
    [Route("api/[controller]")]
    public class ComponentsController : ControllerBase
    {
        private readonly ComponentService componentService;

        public ComponentsController(ComponentService componentService)
        {
            this.componentService = componentService;

        }

        //Get: api/components/
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Component>>> GetAll()
        {
            try
            {
                var components = await componentService.GetAllComponentsAsync();
                return Ok(components);
            }
            catch (Exception ex)
            {
                //create custom error response object to return more structured error information
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Get: api/components/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Component>> GetById(Guid id)
        {
            try
            {
                // Retrieve the component by ID using the service
                var component = await componentService.GetComponentByIdAsync(id);

                // If the component is not found, return a 404 Not Found
                if(component == null)
                {
                    return NotFound($"Component with ID {id} not found.");
                }
                return Ok(component);
            }
            catch (Exception ex)
            {
                // If an error occurs, return a 500 Internal Server Error with the exception message
                //create custom error response object to return more structured error information
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Post: api/components
        [HttpPost]
        public async Task<ActionResult<Component>> Create([FromBody] Component component)
        {
            try
            {
                //call the service to create the component
                var createdComponent = await componentService.CreateComponentAsync(component);

                // Created response with the location of the newly created component
                return CreatedAtAction(nameof(GetById), new { id = createdComponent.Id }, createdComponent);
            }
            catch (ArgumentException ex)
            {
                //create custom error response object to return more structured error information
                return BadRequest($"Invalid input: {ex.Message}");
            }
            catch (Exception ex)
            {
                //create custom error response object to return more structured error information
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //this will be used to update the component information
        //Put: api/components/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Component>> Update(Guid id, [FromBody] Component component)
        {
            try
            {
                //makes sure the ID in the URL matches the ID in the body to prevent accidental updates to the wrong component
                if(id != component.Id)
                    return BadRequest("ID in the URL does not match ID in the body.");

                //call the service to update the component
                var updatedComponent = await componentService.UpdateComponentAsync(component);
                
                // If the component is not found, return a 404 Not Found
                if (updatedComponent == null)
                    return NotFound($"Component with ID {id} not found.");

                return Ok(updatedComponent);

            }
            catch (ArgumentException ex)
            {
                //create custom error response object to return more structured error information
                return BadRequest($"Invalid input: {ex.Message}");
            }
            catch (Exception ex)
            {
                //create custom error response object to return more structured error information
                return StatusCode(500, $"Internal server error: {ex.Message}");

            }
        }

        //this will be used to delete a component from the system
        //delete: api/components/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                //call the service to delete the component
                var deleted = await componentService.DeleteComponentAsync(id);

                if (!deleted)
                    return NotFound($"Component with ID {id} not found.");

                // Return NoContent to indicate successful deletion
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //this will be used to get all components of a specific type, such as all CPUs or all RAM modules
        // Get: api/components/type/{componentType}
        [HttpGet("type/{componentType}")]
        public async Task<ActionResult<IEnumerable<Component>>> GetByComponentType(string componentType)
        {
            try
            {
                var components = await componentService.GetComponentsByTypeAsync(componentType);

                return Ok(components);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //this will be used to get all components from a specific vendor, such as all components from Dell or HP
        // Get: api/components/vendor/{vendorId}
        [HttpGet("vendor/{vendorId}")]
        public async Task<ActionResult<IEnumerable<Component>>> GetByVendor(Guid vendorId)
        {
            try
            {
                var componentsByVendor = await componentService.GetComponentsByVendorIdAsync(vendorId);
                return Ok(componentsByVendor);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }
    }
}
