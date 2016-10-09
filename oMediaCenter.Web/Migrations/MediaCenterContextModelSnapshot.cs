using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using oMediaCenter.Web.Model;

namespace oMediaCenter.Web.Migrations
{
    [DbContext(typeof(MediaCenterContext))]
    partial class MediaCenterContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1");

            modelBuilder.Entity("oMediaCenter.Web.Model.FilePosition", b =>
                {
                    b.Property<string>("FileHash");

                    b.Property<TimeSpan>("LastPlayedPosition");

                    b.HasKey("FileHash");

                    b.ToTable("FilePositions");
                });
        }
    }
}
