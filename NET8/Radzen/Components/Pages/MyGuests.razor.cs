using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace RadzenDemo.Components.Pages
{
    public partial class MyGuests
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public TestDbService TestDbService { get; set; }

        protected IEnumerable<RadzenDemo.Models.TestDb.MyGuest> myGuests;

        protected RadzenDataGrid<RadzenDemo.Models.TestDb.MyGuest> grid0;
        protected override async Task OnInitializedAsync()
        {
            myGuests = await TestDbService.GetMyGuests();
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<AddMyGuest>("Add MyGuest", null);
            await grid0.Reload();
        }

        protected async Task EditRow(RadzenDemo.Models.TestDb.MyGuest args)
        {
            await DialogService.OpenAsync<EditMyGuest>("Edit MyGuest", new Dictionary<string, object> { {"Id", args.Id} });
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, RadzenDemo.Models.TestDb.MyGuest myGuest)
        {
            try
            {
                if (await DialogService.Confirm("Are you sure you want to delete this record?") == true)
                {
                    var deleteResult = await TestDbService.DeleteMyGuest(myGuest.Id);

                    if (deleteResult != null)
                    {
                        await grid0.Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete MyGuest"
                });
            }
        }
    }
}