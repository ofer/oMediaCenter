using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oMediaCenter.Web.Model
{
    public class CachedMediaInformation
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "text")]
        public string Filename { get; set; }

        [Column(TypeName = "text")]
        public string Title { get; set; }

        [Column(TypeName = "text")]
        public string OtherInfo { get; set; }

        [Column(TypeName = "text")]
        public string Year { get; set; }

        [Column(TypeName = "text")]
        public string ImdbNumber { get; set; }

        [Column(TypeName = "text")]
        public string VideoType { get; set; }

        [Column(TypeName = "text")]
        public string Episode { get; set; }

        [Column(TypeName = "text")]
        public string Season { get; set; }

        [Column(TypeName = "text")]
        public string Genres { get; set; }
    }
}
