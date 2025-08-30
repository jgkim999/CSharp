using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private List<Domain.Entities.User> Users = new List<Demo.Domain.Entities.User>();
    private MudDataGrid<Domain.Entities.User> _dataGrid;
    private int _pageSize = 10;

    [Inject] private IDbContextFactory<DemoDbContext> DbFactory { get; set; } = default!;
    
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } 
    
    protected override async Task OnInitializedAsync()
    {
        var storedPageSize = await LocalStorage.GetItemAsync<int>("PageSize");
        if (storedPageSize != 0)
        {
            _pageSize = storedPageSize;
            if (_dataGrid != null)
                await _dataGrid.SetRowsPerPageAsync(_pageSize);
        }
        _dataGrid.PagerStateHasChangedEvent += async () => 
            await OnPageSizeChangedAsync(_dataGrid.RowsPerPage);
    }
    
    private async Task OnPageSizeChangedAsync(int newPageSize)
    {
        _pageSize = newPageSize;
        await LocalStorage.SetItemAsync("PageSize", newPageSize);
    }
    
    private async Task<GridData<Domain.Entities.User>> LoadGridData(GridState<Domain.Entities.User> state)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        var result = await db.Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();
        
        var totalCount = await db.Users.CountAsync();
        
        GridData<Domain.Entities.User> data = new()
        {
            Items = result,
            TotalItems = totalCount
        };
        return data;
    }
}
