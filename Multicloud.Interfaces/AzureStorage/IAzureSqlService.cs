using Multicloud.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Interfaces.AzureStorage
{
    public interface IAzureSqlService
    {
        Task<List<Region>> GetRegionsAsync();
    }
}
