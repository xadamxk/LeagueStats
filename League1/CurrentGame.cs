using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace League1
{
    public class Rune
    {
        public int Count { get; set; }
        public int RuneId { get; set; }
    }

    public class Mastery
    {
        public int Rank { get; set; }
        public int MasteryId { get; set; }
    }

    public class Participant
    {
        public int TeamId { get; set; }
        public int Spell1Id { get; set; }
        public int Spell2Id { get; set; }
        public int ChampionId { get; set; }
        public int ProfileIconId { get; set; }
        public string SummonerName { get; set; }
        public bool Bot { get; set; }
        public int SummonerId { get; set; }
        public List<Rune> Runes { get; set; }
        public List<Mastery> Masteries { get; set; }
    }

    public class Observers
    {
        public string EncryptionKey { get; set; }
    }

    public class BannedChampion
    {
        public int ChampionId { get; set; }
        public int TeamId { get; set; }
        public int PickTurn { get; set; }
    }

    public class CurrentGame
    {
        public int GameId { get; set; }
        public int MapId { get; set; }
        public string GameMode { get; set; }
        public string GameType { get; set; }
        public int GameQueueConfigId { get; set; }
        public List<Participant> Participants { get; set; }
        public Observers Observers { get; set; }
        public string PlatformId { get; set; }
        public List<BannedChampion> BannedChampions { get; set; }
        public long GameStartTime { get; set; }
        public int GameLength { get; set; }
    }
}
