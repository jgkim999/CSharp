using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using RadzenDemo.Data;

namespace RadzenDemo.Controllers
{
    public partial class ExportTestDbController : ExportController
    {
        private readonly TestDbContext context;
        private readonly TestDbService service;

        public ExportTestDbController(TestDbContext context, TestDbService service)
        {
            this.service = service;
            this.context = context;
        }

        [HttpGet("/export/TestDb/myguests/csv")]
        [HttpGet("/export/TestDb/myguests/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportMyGuestsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetMyGuests(), Request.Query, false), fileName);
        }

        [HttpGet("/export/TestDb/myguests/excel")]
        [HttpGet("/export/TestDb/myguests/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportMyGuestsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetMyGuests(), Request.Query, false), fileName);
        }

        [HttpGet("/export/TestDb/useraccounts/csv")]
        [HttpGet("/export/TestDb/useraccounts/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportUserAccountsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetUserAccounts(), Request.Query, false), fileName);
        }

        [HttpGet("/export/TestDb/useraccounts/excel")]
        [HttpGet("/export/TestDb/useraccounts/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportUserAccountsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetUserAccounts(), Request.Query, false), fileName);
        }
    }
}
