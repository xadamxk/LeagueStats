using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace League1
{
    class League
    {
        public List<SummonerName> SummonerName { get; set; }
    }

    public class Entry
    {
        public int leaguePoints { get; set; }
        public bool isFreshBlood { get; set; }
        public bool isHotStreak { get; set; }
        public string division { get; set; }
        public bool isInactive { get; set; }
        public bool isVeteran { get; set; }
        public int losses { get; set; }
        public string playerOrTeamName { get; set; }
        public string playerOrTeamId { get; set; }
        public int wins { get; set; }
    }

    public class SummonerName
    {
        public string queue { get; set; }
        public string name { get; set; }
        public List<Entry> entries { get; set; }
        public string tier { get; set; }
    }

}
