using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
    public class MediaCenterContext : DbContext
    {
        public MediaCenterContext(DbContextOptions<MediaCenterContext> options) : base(options) { }

        public DbSet<FilePosition> FilePositions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=oMediaCenter.db");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
