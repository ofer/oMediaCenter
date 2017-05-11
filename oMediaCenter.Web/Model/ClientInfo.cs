using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oMediaCenter.Web.Model
{
    public class ClientInfo
    {
        public ClientInfo(DateTime lastPoll)
        {
            LatestCommand = new ClientCommand();
            LastPoll = lastPoll;
        }

        public ClientCommand LatestCommand { get; set; }
        public DateTime LastPoll { get; set; }
    }
}
