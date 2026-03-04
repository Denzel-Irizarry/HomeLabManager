using HomeLabManager.Core.Entities;
namespace HomeLabManager.API.Interfaces
{
    public interface ComponentRepositoryInterface
    {
        Task<IEnumerable<Component>> GetAllAsync();
        Task<Component?> GetByIdAsync(Guid id);
        Task<Component?> CreateAsync(Component component);
        Task<Component?> UpdateAsync(Component component);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<Component>>GetByTypeAsync(string componentType);
        Task<IEnumerable<Component>> GetByVendorIdAsync(Guid vendorId);

    }
}
