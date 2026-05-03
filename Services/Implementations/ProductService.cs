using ProductRepositoryPattern.DTOs;
using ProductRepositoryPattern.Models;
using ProductRepositoryPattern.Repositories.Interfaces;
using ProductRepositoryPattern.Services.Interfaces;

namespace ProductRepositoryPattern.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
        {
            var products = await _uow.Products.GetAllAsync();
            return products.Select(MapToDto);
        }
        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            return product is null ? null : MapToDto(product);
        }
        public async Task<ProductResponseDto> CreaetProductAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
            };
            await _uow.Products.AddAsync(product);
            await _uow.CompleteAsync();

            return MapToDto(product);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product is null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.IsActive = dto.IsActive;

            _uow.Products.Update(product);
            await _uow.CompleteAsync();

            return MapToDto(product);
        }
        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product is null) return false;
            _uow.Products.Delete(product);
            await _uow.CompleteAsync();

            return true;  
        }


        // Private mapping to keep the model off the wire

        private static ProductResponseDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
        };
    }
}
