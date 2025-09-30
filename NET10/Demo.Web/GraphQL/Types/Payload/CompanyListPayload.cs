using Demo.Application.DTO.Company;

namespace Demo.Web.GraphQL.Types.Payload;

public record CompanyListPayload(
    List<CompanyDto>? Items,
    int TotalItems,
    IList<string>? Errors = null);
