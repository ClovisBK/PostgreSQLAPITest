namespace ProductRepositoryPattern.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
       IProductRepository Products { get; }
        Task<int> CompleteAsync();
    }
}
