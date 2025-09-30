using Demo.Application.DTO.Product;

namespace Demo.Web.GraphQL.Types.Payload;

public record ProductListPayload(
    List<ProductDto>? Items,
    int TotalItems,
    int Page,
    int PageSize,
    IList<string>? Errors = null);
