using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private List<Domain.Entities.User> Users = new List<Demo.Domain.Entities.User>();
   
    [Inject]
    private IDbContextFactory<DemoDbContext> DbFactory { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Users = await db.Users.AsNoTracking().Take(5).ToListAsync();
    }
    
    private async Task<GridData<Domain.Entities.User>> LoadGridData(GridState<Domain.Entities.User> state)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var result = await db.Users.AsNoTracking().Skip(state.Page * state.PageSize).Take(state.PageSize).ToListAsync();
        var totalCount = await db.Users.CountAsync();
        GridData<Domain.Entities.User> data = new()
        {
            Items = result,
            TotalItems = totalCount
        };
        return data;
    }
}
