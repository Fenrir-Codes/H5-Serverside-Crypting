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
                // Concatenate Title and Description
                string concatenatedString = string.Concat(todo.Title, todo.Description);

                // Calculate MD5 hash from the concatenated string
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }

                    // Set the HashValue property to the computed MD5 hash
                    todo.HashValue = sb.ToString();
                }

                // Encrypt Title and Description before storing in the database
                todo.Title = _dataProtector.Protect(todo.Title);
                todo.Description = _dataProtector.Protect(todo.Description);

                _context.toDo.Add(todo); // Add input to context variables
                await _context.SaveChangesAsync(); // Save data

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

        #region Update function with hashCheck
        public async Task<string> Update(toDoModel todo)
        {
            try
            {
                var entry = await _context.toDo.FindAsync(todo.id);

                // If the response is not null
                if (entry != null)
                {
                    // Calculate the new MD5 hash from the concatenated string
                    using (MD5 md5 = MD5.Create())
                    {
                        // Concatenate the new Title and Description
                        string concatenatedString = string.Concat(todo.Title, todo.Description);

                        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < hashBytes.Length; i++)
                        {
                            sb.Append(hashBytes[i].ToString("x2"));
                        }

                        // Compare the new hash with the existing HashValue
                        if (sb.ToString() != entry.HashValue)
                        {
                            // The hash values are different, indicating changes in Title or Description
                            hasChanges = true;

                            // Update the Title and Description, and save the changes
                            entry.Title = _dataProtector.Protect(todo.Title);
                            entry.Description = _dataProtector.Protect(todo.Description);
                            entry.HashValue = sb.ToString(); // Update the HashValue

                            _context.Entry(entry).State = EntityState.Modified;
                            await _context.SaveChangesAsync();

                            return "Update successful";
                        }
                        else
                        {
                            return "No changes made"; // Return a message when no changes were made
                        }
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

    }
}
