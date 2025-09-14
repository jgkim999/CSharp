using Demo.Admin.Models;

namespace Demo.Admin.Services;

public interface IProductService
{
    Task<bool> CreateProductAsync(ProductCreateModel model);
}