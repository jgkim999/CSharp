using FastEndpoints;

namespace Demo.Web.Endpoints.Product;

/// <summary>
/// Product group
/// </summary>
/// <seealso cref="Group" />
public sealed class ProductGroup : Group
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
