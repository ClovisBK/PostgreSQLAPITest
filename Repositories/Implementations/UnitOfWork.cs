using ProductRepositoryPattern.Data;
using ProductRepositoryPattern.Repositories.Interfaces;

namespace ProductRepositoryPattern.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IProductRepository Products {  get;  private set; }

        public UnitOfWork(AppDbContext context)
        {
             _context = context;
            Products = new ProductRepository(context);
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
            
        public void Dispose() => _context.Dispose();
    
    }
}
