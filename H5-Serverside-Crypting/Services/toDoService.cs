using H5_Serverside_Crypting.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace H5_Serverside_Crypting.Services
{
    public class toDoService
    {
        private string _secret;
        private readonly toDoContext _context;
        private readonly IDataProtector _dataProtector;
        private bool hasChanges = false;

        #region constructor
        public toDoService(toDoContext context, IDataProtectionProvider dataProtectionProvider, IConfiguration Configuration)
        {
            //Get the protection key from the user secrets
            _secret = Configuration["ProtectorSecretKey"];

            _context = context;
            _dataProtector = dataProtectionProvider.CreateProtector(_secret);
        }
        #endregion

        #region create function async
        public async Task<string> Create(toDoModel todo)
        {
            try
            {
                // Encrypting data before storing in the database
                todo.Title = _dataProtector.Protect(todo.Title);
                todo.Description = _dataProtector.Protect(todo.Description);

                _context.toDo.Add(todo); // add input to context variables
                await _context.SaveChangesAsync(); // save data

                return $"New entry created";
            }
            catch (Exception ex)
            {
                // Handle the exception and return an error message
                return $"Error: {ex.Message}";
            }
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
        public async Task<string> Update(toDoModel todo)
        {
            try
            {
                var entry = await _context.toDo.FindAsync(todo.id);

                // If the response is not null
                if (entry != null)
                {
                    //First we have to unprotect the string for compilation what we have
                    var title = _dataProtector.Unprotect(entry.Title);
                    var desc = _dataProtector.Unprotect(entry.Description);

                    var changedEntry = new toDoModel
                    {
                        id = entry.id,
                        Title = entry.Title,
                        Description = entry.Description,
                        UserId = entry.UserId
                    };


                    // If the response Title has changed, the protector will encrypt the changed input
                    if (title != todo.Title || desc != todo.Description)
                    {
                        hasChanges = true;

                        if (entry.Title != todo.Title)
                        {
                            changedEntry.Title = _dataProtector.Protect(todo.Title);
                        }

                        if (entry.Description != todo.Description)
                        {
                            changedEntry.Description = _dataProtector.Protect(todo.Description);
                        }

                        _context.Entry(entry).State = EntityState.Detached;
                        _context.Entry(changedEntry).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        return "Update successful";
                    }
                    else
                    {
                        return "No changes made"; // Return a message when no changes were made
                    }
                }

                return "Entry not found"; // Return a message when the entry is not found
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}"; // Return an error message if an exception occurs
            }
        }
        #endregion

        #region Delete function async
        public async Task<string> Delete(toDoModel entry)
        {
            try
            {
                var entryToRemove = _context.toDo.Local.FirstOrDefault(e => e.id == entry.id);
                if (entryToRemove != null)
                {
                    _context.Entry(entryToRemove).State = EntityState.Detached;
                }
                _context.toDo.Remove(entry);
                await _context.SaveChangesAsync();

                return "Delete successful";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        #endregion

        #region Hash function
        public async Task<string> MD5Hashing(string input, string previousHash)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    string Hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

                    if (Hash != previousHash)
                    {
                        // Hash has changed, you may want to update the previousHash value here.
                        // For example, if you have a database, you can store the newHash there.
                        return $"{Hash}";
                    }
                    else
                    {
                        return $"{Hash} There was no change in hashing.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred! {ex.Message}";
            }
        }
        #endregion

    }
}
