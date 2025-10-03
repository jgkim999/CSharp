using FastEndpoints;

namespace Demo.Web.Endpoints.Product;

public class ProductGroup : Group
{
    /// <summary>
    /// Product group
    /// </summary>
    public ProductGroup()
    {
        Configure(
            "",
            ep =>
            {
                ep.Description(
                    x => x.Produces(401)
                        .WithTags("Product"));
            });
    }
}
