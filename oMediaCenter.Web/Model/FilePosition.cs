using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
    public class FilePosition
    {
        [Key]
        public string FileHash { get; set; }
        public TimeSpan LastPlayedPosition { get; set; }
    }
}
