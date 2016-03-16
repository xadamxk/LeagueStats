using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace League1
{
    class RankedStats
    {
        public long modifyDate { get; set; }
        public List<Champion> champions { get; set; }
        public int summonerId { get; set; }
    }


    public class Stats
    {
        public int totalDeathsPerSession { get; set; }
        public int totalSessionsPlayed { get; set; }
        public int totalDamageTaken { get; set; }
        public int totalQuadraKills { get; set; }
        public int totalTripleKills { get; set; }
        public int totalMinionKills { get; set; }
        public int maxChampionsKilled { get; set; }
        public int totalDoubleKills { get; set; }
        public int totalPhysicalDamageDealt { get; set; }
        public int totalChampionKills { get; set; }
        public int totalAssists { get; set; }
        public int mostChampionKillsPerSession { get; set; }
        public int totalDamageDealt { get; set; }
        public int totalFirstBlood { get; set; }
        public int totalSessionsLost { get; set; }
        public int totalSessionsWon { get; set; }
        public int totalMagicDamageDealt { get; set; }
        public int totalGoldEarned { get; set; }
        public int totalPentaKills { get; set; }
        public int totalTurretsKilled { get; set; }
        public int mostSpellsCast { get; set; }
        public int maxNumDeaths { get; set; }
        public int totalUnrealKills { get; set; }
        public int maxLargestCriticalStrike { get; set; }
        public int rankedPremadeGamesPlayed { get; set; }
        public double totalNeutralMinionsKilled { get; set; }
        public int maxLargestKillingSpree { get; set; }
        public int maxTimeSpentLiving { get; set; }
        public int maxTimePlayed { get; set; }
        public int botGamesPlayed { get; set; }
        public int killingSpree { get; set; }
        public int rankedSoloGamesPlayed { get; set; }
        public double totalHeal { get; set; }
        public int normalGamesPlayed { get; set; }
    }

    public class Champion
    {
        public int id { get; set; }
        public Stats stats { get; set; }
    }

}
