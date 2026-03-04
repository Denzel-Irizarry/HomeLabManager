using HomeLabManager.Core.Entities;
using HomeLabManager.API.Interfaces;

namespace HomeLabManager.API.Services
{
    public class ComponentService
    {
        /* ***Why have a service layer?***
         * Business logic: Validation, ID generation, rules
         * Reusability: Multiple controllers can use the same service
         * Validates input before calling the repository
         * Generates new GUIDs for creates
         * Handles edge cases (empty GUIDs, null strings)
         * Throws exceptions for invalid inputs (controller will catch these)
         */
        private readonly ComponentRespositoryInterface _repository;

        // instance of ComponentRespositoryInterface as a parameter and assigns it to the private field This allows the service to interact with the data repository for components, enabling it to perform operations such as retrieving, creating, updating, and deleting components from the database.
        public ComponentService(ComponentRespositoryInterface repository)
        {
            _repository = repository;
        }

        // retrieves all components from the repository. It calls the GetAllAsync method of the repository, which returns a list of components, and then returns that list to the caller. This allows clients of the ComponentService to obtain a complete list of all components stored in the database.
        public async Task<IEnumerable<Component>> GetAllComponentsAsync()
        {
            return await _repository.GetAllAsync();
        }

        // retrieves a specific component by (ID). It takes the ID of the component to be retrieved. The method calls the GetByIdAsync method of the repository, passing the ID as an argument, and returns the resulting component. 
        public async Task<Component?> GetComponentByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // creating a new component in the repository. It takes a Component object as a parameter, validates it, and then calls the CreateAsync method of the repository to save it to the database. 
        public async Task<Component?> CreateComponentAsync(Component component)
        {
            // Validate the component before creating it also need to create custom exceptions 
            if (string.IsNullOrWhiteSpace(component.Name))
                throw new ArgumentException("Component name is required.");

            //generate a new ID if not provided
            if (component.Id == Guid.Empty)
                component.Id = Guid.NewGuid();

            return await _repository.CreateAsync(component);
        }

        public async Task<Component?> UpdateComponentAsync(Component component)
        {
            // Validate the component before updating it
            if (component.Id == Guid.Empty)
                throw new ArgumentException("Component ID is required for update.");
            if (string.IsNullOrWhiteSpace(component.Name))
                throw new ArgumentException("Component name is required.");

            return await _repository.UpdateAsync(component);
        }

        public async Task<bool> DeleteComponentAsync(Guid id)
        {
            // Validate the ID before attempting to delete the component
            if (id == Guid.Empty)
                throw new ArgumentException("Component ID is required for deletion.");

            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Component>> GetComponentsByTypeAsync(string componentType)
        {
            // Validate the component type before retrieving components
            if (string.IsNullOrWhiteSpace(componentType))
                throw new ArgumentException("Component type is required.");

            return await _repository.GetByTypeAsync(componentType);
        }

        public async Task<IEnumerable<Component>> GetComponentsByVendorIdAsync(Guid vendorId)
        {
            // Validate the vendor ID before retrieving components
            if (vendorId == Guid.Empty)
                throw new ArgumentException("Vendor ID is required.");
            return await _repository.GetByVendorIdAsync(vendorId);
        }

    }
}
