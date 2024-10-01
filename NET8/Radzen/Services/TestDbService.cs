using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen;

using RadzenDemo.Data;

namespace RadzenDemo
{
    public partial class TestDbService
    {
        TestDbContext Context
        {
           get
           {
             return this.context;
           }
        }

        private readonly TestDbContext context;
        private readonly NavigationManager navigationManager;

        public TestDbService(TestDbContext context, NavigationManager navigationManager)
        {
            this.context = context;
            this.navigationManager = navigationManager;
        }

        public void Reset() => Context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);

        public void ApplyQuery<T>(ref IQueryable<T> items, Query query = null)
        {
            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }
        }


        public async Task ExportMyGuestsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/testdb/myguests/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/testdb/myguests/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportMyGuestsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/testdb/myguests/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/testdb/myguests/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnMyGuestsRead(ref IQueryable<RadzenDemo.Models.TestDb.MyGuest> items);

        public async Task<IQueryable<RadzenDemo.Models.TestDb.MyGuest>> GetMyGuests(Query query = null)
        {
            var items = Context.MyGuests.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnMyGuestsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnMyGuestGet(RadzenDemo.Models.TestDb.MyGuest item);
        partial void OnGetMyGuestById(ref IQueryable<RadzenDemo.Models.TestDb.MyGuest> items);


        public async Task<RadzenDemo.Models.TestDb.MyGuest> GetMyGuestById(uint id)
        {
            var items = Context.MyGuests
                              .AsNoTracking()
                              .Where(i => i.Id == id);

 
            OnGetMyGuestById(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnMyGuestGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnMyGuestCreated(RadzenDemo.Models.TestDb.MyGuest item);
        partial void OnAfterMyGuestCreated(RadzenDemo.Models.TestDb.MyGuest item);

        public async Task<RadzenDemo.Models.TestDb.MyGuest> CreateMyGuest(RadzenDemo.Models.TestDb.MyGuest myguest)
        {
            OnMyGuestCreated(myguest);

            var existingItem = Context.MyGuests
                              .Where(i => i.Id == myguest.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.MyGuests.Add(myguest);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(myguest).State = EntityState.Detached;
                throw;
            }

            OnAfterMyGuestCreated(myguest);

            return myguest;
        }

        public async Task<RadzenDemo.Models.TestDb.MyGuest> CancelMyGuestChanges(RadzenDemo.Models.TestDb.MyGuest item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnMyGuestUpdated(RadzenDemo.Models.TestDb.MyGuest item);
        partial void OnAfterMyGuestUpdated(RadzenDemo.Models.TestDb.MyGuest item);

        public async Task<RadzenDemo.Models.TestDb.MyGuest> UpdateMyGuest(uint id, RadzenDemo.Models.TestDb.MyGuest myguest)
        {
            OnMyGuestUpdated(myguest);

            var itemToUpdate = Context.MyGuests
                              .Where(i => i.Id == myguest.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(myguest);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterMyGuestUpdated(myguest);

            return myguest;
        }

        partial void OnMyGuestDeleted(RadzenDemo.Models.TestDb.MyGuest item);
        partial void OnAfterMyGuestDeleted(RadzenDemo.Models.TestDb.MyGuest item);

        public async Task<RadzenDemo.Models.TestDb.MyGuest> DeleteMyGuest(uint id)
        {
            var itemToDelete = Context.MyGuests
                              .Where(i => i.Id == id)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnMyGuestDeleted(itemToDelete);


            Context.MyGuests.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterMyGuestDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportUserAccountsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/testdb/useraccounts/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/testdb/useraccounts/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportUserAccountsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/testdb/useraccounts/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/testdb/useraccounts/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnUserAccountsRead(ref IQueryable<RadzenDemo.Models.TestDb.UserAccount> items);

        public async Task<IQueryable<RadzenDemo.Models.TestDb.UserAccount>> GetUserAccounts(Query query = null)
        {
            var items = Context.UserAccounts.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnUserAccountsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnUserAccountGet(RadzenDemo.Models.TestDb.UserAccount item);
        partial void OnGetUserAccountByUserId(ref IQueryable<RadzenDemo.Models.TestDb.UserAccount> items);


        public async Task<RadzenDemo.Models.TestDb.UserAccount> GetUserAccountByUserId(long userid)
        {
            var items = Context.UserAccounts
                              .AsNoTracking()
                              .Where(i => i.UserId == userid);

 
            OnGetUserAccountByUserId(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnUserAccountGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnUserAccountCreated(RadzenDemo.Models.TestDb.UserAccount item);
        partial void OnAfterUserAccountCreated(RadzenDemo.Models.TestDb.UserAccount item);

        public async Task<RadzenDemo.Models.TestDb.UserAccount> CreateUserAccount(RadzenDemo.Models.TestDb.UserAccount useraccount)
        {
            OnUserAccountCreated(useraccount);

            var existingItem = Context.UserAccounts
                              .Where(i => i.UserId == useraccount.UserId)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.UserAccounts.Add(useraccount);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(useraccount).State = EntityState.Detached;
                throw;
            }

            OnAfterUserAccountCreated(useraccount);

            return useraccount;
        }

        public async Task<RadzenDemo.Models.TestDb.UserAccount> CancelUserAccountChanges(RadzenDemo.Models.TestDb.UserAccount item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnUserAccountUpdated(RadzenDemo.Models.TestDb.UserAccount item);
        partial void OnAfterUserAccountUpdated(RadzenDemo.Models.TestDb.UserAccount item);

        public async Task<RadzenDemo.Models.TestDb.UserAccount> UpdateUserAccount(long userid, RadzenDemo.Models.TestDb.UserAccount useraccount)
        {
            OnUserAccountUpdated(useraccount);

            var itemToUpdate = Context.UserAccounts
                              .Where(i => i.UserId == useraccount.UserId)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(useraccount);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterUserAccountUpdated(useraccount);

            return useraccount;
        }

        partial void OnUserAccountDeleted(RadzenDemo.Models.TestDb.UserAccount item);
        partial void OnAfterUserAccountDeleted(RadzenDemo.Models.TestDb.UserAccount item);

        public async Task<RadzenDemo.Models.TestDb.UserAccount> DeleteUserAccount(long userid)
        {
            var itemToDelete = Context.UserAccounts
                              .Where(i => i.UserId == userid)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnUserAccountDeleted(itemToDelete);


            Context.UserAccounts.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterUserAccountDeleted(itemToDelete);

            return itemToDelete;
        }
        }
}