using H5_Serverside_Crypting.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;

namespace H5_Serverside_Crypting.Services
{
    public class toDoService
    {
        private readonly string _secret;
        private readonly toDoContext _context;
        private readonly IDataProtector _dataProtector;

        #region constructor
        public toDoService(toDoContext context, IDataProtectionProvider dataProtectionProvider, IConfiguration Configuration)
        {
            //Get the protection  key from the user secrets
            _secret = Configuration["ProtectorSecretKey"];

            _context = context;
            _dataProtector = dataProtectionProvider.CreateProtector(_secret);
        }
        #endregion

        #region create function async
        public async Task Create(toDoModel todo)
        {
            // Encrypting data before storing in the database
            todo.Title = _dataProtector.Protect(todo.Title);
            todo.Description = _dataProtector.Protect(todo.Description);

            _context.toDo.Add(todo); // add input to context variables
            await _context.SaveChangesAsync(); // save data
        }
        #endregion

        #region Get request async
        public async Task<List<toDoModel>> ReadData(ClaimsPrincipal _user)
        {
            //Get all the dsata from te todo table
            var response = await _context.toDo.AsNoTracking().Where(user => user.UserId
                == Guid.Parse(_user.FindFirstValue(ClaimTypes.NameIdentifier))).ToListAsync();

            //Decrypt data after retreiving it
            response.ForEach(res =>
            {
                //All the data exists in the table will be decrypted
                res.Title = _dataProtector.Unprotect(res.Title);
                res.Description = _dataProtector.Unprotect(res.Description);
            });

            return response; // return the decrypted data
        }
        #endregion

        #region Update async function
        public async Task Update(toDoModel todo)
        {
            ////Get the requested data with the id from the dastabase
            //var res = await _context.toDo.FindAsync(todo.id);

            ////If response not null
            //if (res != null)
            //{
            //    //if response Title and Desc.changed the protector vill crypting the input
            //    res.Title = _dataProtector.Protect(todo.Title);
            //    res.Description = _dataProtector.Protect(todo.Description);

            //    await _context.SaveChangesAsync(); // save changes
            //}

            //Search for existing entry with id
            var entry = await _context.toDo.FindAsync(todo.id);

            //If response not null
            if (entry != null)
            {
                var changerEntry = new toDoModel
                {
                    id = entry.id,
                    Title = entry.Title,
                    Description = entry.Description,
                    UserId = entry.UserId
                };

                //if response Title and Desc.changed the protector vill crypting the input
                bool hasChanges = false;
                if (entry.Title != todo.Title)
                {
                    changerEntry.Title = _dataProtector.Protect(todo.Title);
                    hasChanges = true;
                }
                if (entry.Description != todo.Description)
                {
                    changerEntry.Description = _dataProtector.Protect(todo.Description);
                    hasChanges = true;
                }
                if (hasChanges)
                {
                    _context.Entry(entry).State = EntityState.Detached;
                    _context.Entry(changerEntry).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }
        }
        #endregion

        #region Delete function async
        public async Task Delete(toDoModel entry)
        {
            var entryToRemove = _context.toDo.Local.FirstOrDefault(e => e.id == entry.id);
            if (entryToRemove != null)
            {
                _context.Entry(entryToRemove).State = EntityState.Detached;
            }
            _context.toDo.Remove(entry);
            await _context.SaveChangesAsync();

        }
        #endregion

    }
}
