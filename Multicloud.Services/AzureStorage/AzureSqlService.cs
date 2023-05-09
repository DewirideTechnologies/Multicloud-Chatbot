using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Multicloud.Data.Models;
using Multicloud.Interfaces.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multicloud.Services.AzureStorage
{
    public class AzureSqlService : IAzureSqlService
    {
        private readonly IConfiguration _configuration;
        MulticloudDbContext context;
        public MulticloudDbContext _context {  get { return context; } }
        public AzureSqlService(IConfiguration configuration)
        {
            _configuration = configuration;
            context = new MulticloudDbContext(_configuration);
        }

        // read data from azure sql database using entity framework
        public async Task<List<Region>> GetRegionsAsync()
        {
            return await _context.Regions.ToListAsync();
        }

        // get region by id
        public async Task<Region> GetRegionByIdAsync(int id)
        {
            return await _context.Regions.FindAsync(id);
        }

    }
}
