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
    public partial class EditMyGuest
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

        [Parameter]
        public uint Id { get; set; }

        protected override async Task OnInitializedAsync()
        {
            myGuest = await TestDbService.GetMyGuestById(Id);
        }
        protected bool errorVisible;
        protected RadzenDemo.Models.TestDb.MyGuest myGuest;

        protected async Task FormSubmit()
        {
            try
            {
                await TestDbService.UpdateMyGuest(Id, myGuest);
                DialogService.Close(myGuest);
            }
            catch (Exception ex)
            {
                errorVisible = true;
            }
        }

        protected async Task CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}