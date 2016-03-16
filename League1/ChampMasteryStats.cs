using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace League1
{
    class ChampMasteryStats
    {
        public int ChampionPoints { get; set; }
        public int PlayerId { get; set; }
        public int ChampionPointsUntilNextLevel { get; set; }
        public bool ChestGranted { get; set; }
        public int ChampionLevel { get; set; }
        public int ChampionId { get; set; }
        public int ChampionPointsSinceLastLevel { get; set; }
        public long LastPlayTime { get; set; }
    }
}
