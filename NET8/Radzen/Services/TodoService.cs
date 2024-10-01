using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

using RadzenDemo.Models;

namespace RadzenDemo
{
    public partial class TodoService
    {
        private readonly HttpClient httpClient;
        private readonly NavigationManager navigationManager;

        public TodoService(NavigationManager navigationManager, IHttpClientFactory httpClientFactory)
        {
            this.httpClient = httpClientFactory.CreateClient("Todo");
            this.navigationManager = navigationManager;
        }
    }
}