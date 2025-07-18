using ARM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ARM.Services;

public interface IDBService
{
    public bool CheckConnection();
    public string GetConnectionString();
    public Task<int?> Login(string login, string passord);       
    public Task<List<ARMReport>> GetAllReportsAsync();        
    //Task<List<Dictionary<string, object>>> GetUserAccountsAsync();
    //Task<string> PostgreSqlVersionAsync();
    //Task<bool> NewItemAsync(string name, bool usertyped, int datatype, int owners);
    Task UpdatePostAsync(PostModel post);            
}