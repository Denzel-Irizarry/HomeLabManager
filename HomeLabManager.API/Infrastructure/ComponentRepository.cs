using HomeLabManager.Core.Entities;
using HomeLabManager.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeLabManager.API.Infrastructure
{
    public class ComponentRepository : ComponentRespositoryInterface
    {
        private readonly ApplicationDBContext _context;

        public ComponentRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Component>> GetAllAsync()
        {
            return await _context.Components.ToListAsync();
        }

        public async Task<Component?> GetByIdAsync(Guid id)
        {
            return await _context.Components
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Component?> CreateAsync(Component component)
        {
            _context.Components.Add(component);
            await _context.SaveChangesAsync();
            return component;
        }

        public async Task<Component?> UpdateAsync(Component component)
        {
            // Check if the component exists before updating
            var existing = await _context.Components.FindAsync(component.Id);
            if (existing == null)
                return null;

            // Update the existing component's properties
            existing.Name = component.Name; 
            existing.ComponentType = component.ComponentType;
            existing.Manufacturer = component.Manufacturer;
            existing.ModelNumber = component.ModelNumber;
            existing.Specifications = component.Specifications;
            existing.UnitPrice = component.UnitPrice;
            existing.VendorId = component.VendorId;

            // Mark the entity as modified and save changes
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            // Check if the component exists before deleting
            var component = await _context.Components.FindAsync(id);
            if (component == null)
                return false;

            // Remove the component and save changes
            _context.Components.Remove(component);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Component>> GetByTypeAsync(string componentType)
        {
            // Retrieve components that match the specified component type
            return await _context.Components
                .Where(c => c.ComponentType == componentType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Component>> GetByVendorIdAsync(Guid vendorId)
        {
            // Retrieve components that are associated with the specified vendor ID
            return await _context.Components
                .Where(c => c.VendorId == vendorId)
                .ToListAsync();
        }

    }
}
