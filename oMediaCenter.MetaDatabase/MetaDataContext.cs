using Microsoft.EntityFrameworkCore;

namespace oMediaCenter.MetaDatabase
{

	public class MetaDataContext : DbContext
	{
		public MetaDataContext()
		{

		}
		public MetaDataContext(DbContextOptions<MetaDataContext> options) : base(options) { }

		public DbSet<MediaData> MediaDatum { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Filename=mediaMetaData.db");
			base.OnConfiguring(optionsBuilder);
		}
	}
}
