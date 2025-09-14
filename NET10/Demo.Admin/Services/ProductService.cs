using Demo.Admin.Models;
using RestSharp;
using System.Text.Json;

namespace Demo.Admin.Services;

public class ProductService : IProductService
{
    private readonly RestClient _restClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(RestClient restClient, ILogger<ProductService> logger)
    {
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<bool> CreateProductAsync(ProductCreateModel model)
    {
        try
        {
            var request = new RestRequest("api/product/create", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            var requestObject = new
            {
                CompanyId = model.CompanyId,
                Name = model.Name,
                Price = model.Price
            };

            request.AddJsonBody(requestObject);

            _logger.LogInformation("상품 생성 요청: {Name}, 회사ID: {CompanyId}, 가격: {Price}",
                model.Name, model.CompanyId, model.Price);

            var response = await _restClient.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                _logger.LogInformation("상품 생성 성공: {Name}", model.Name);
                return true;
            }
            else
            {
                var errorContent = response.Content ?? "알 수 없는 오류";
                _logger.LogError("상품 생성 실패 - HTTP {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "상품 생성 중 예외 발생: {Name}", model.Name);
            return false;
        }
    }
}