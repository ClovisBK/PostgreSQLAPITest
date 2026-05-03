using System.Security.AccessControl;
using Microsoft.EntityFrameworkCore;
using ProductRepositoryPattern.Data;
using ProductRepositoryPattern.Models;
using ProductRepositoryPattern.Repositories.Interfaces;

namespace ProductRepositoryPattern.Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Product product) => await _context.Products.AddAsync(product);

        public async Task<Product?> GetByIdAsync(int id) => await _context.Products.FindAsync(id);

        public async Task<IEnumerable<Product>> GetAllAsync() => await _context.Products.ToListAsync();

        public async Task<IEnumerable<Product>> GetActiveAsync()
            => await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        public void Update(Product product) => _context.Products.Update(product);

        public void Delete(Product product) => _context.Products.Remove(product);

        public async Task<bool> ExistsAsync(int id) => await _context.Products.AnyAsync(p => p.Id == id);
       
    }
}
