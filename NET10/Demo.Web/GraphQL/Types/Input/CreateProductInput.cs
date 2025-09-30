namespace Demo.Web.GraphQL.Types.Input;

public record CreateProductInput(long CompanyId, string Name, int Price);
