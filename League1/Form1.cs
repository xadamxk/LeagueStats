using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

// TODO
// 1. Add check for sea platform (??? - ln 47)
// 2. Auto grab usernames: C:\Riot Games\League of Legends\RADS\projects\lol_air_client\releases\0.0.1.187\deploy\preferences
    // or save as text file
// 3. Save dev key as text file
// DONE. Hide all panels, shrink window, after data is filled, show panels according to enemy count & grow window size.
// DONE. Add "Team" & "Enemy" buttons instead of search.
// 6. Load new window on champion name click or open their champion.gg page
// 7. add other spells (http://leagueoflegends.wikia.com/wiki/Spells)
// BUG
// 1. Ranked stats don't come back occasionally (spaces in name?)



namespace League1
{
    public partial class Form1 : Form
    {
        // temp
        public static string apiKey = "";

        // Variables
        public static string summonerName;
        public static string summonerID;
        public static int enemyTeamID;
        public bool searchEnemy = true;

        public string[] enemySummonerNameArray;
        public int[] enemySummonerIdArray;
        public int[] enemySummonerSpellId1;
        public int[] enemySummonerSpellId2;
        public int[] enemySummonerChampionId;

        public static string server;
        public static string[] ServerList =
        {
            "br", "eune", "euw", "lan", "las",
            "na", "oce", "ru", "tr", "sea",
            "kr"
        }; // removed pbe due to error


        public static string platform;
        public static string[] PlatformList =
        {
            "BR1", "EUN1", "EUW1", "LA1", "LA2",
            "NA1", "OC1", "RU", "TR1", "???",
            "KR"
        };

        public static string[,] SummonerResponceErrors =
        {
            {"400", "Bad Request. Try Again Shortly."},
            {"401", "Unathorized Request."},
            {"404", "No Summoner Data Found. (Check Spelling/Internet Connection)"},
            {"429", "Developer Rate Limit Exceeded."},
            {"500", "Internal Sever Error. Try Again Shortly."},
            {"503", "Service Unavailable. Refer To 'https://developer.riotgames.com/status' For More Info."}
        };

        public static string[,] CurrentGameResponceErrors =
        {
            {"403", "Forbidden. Access Denied."},
            {"404", "No Summoner Data Found/ Not In Game. (Check Internet Connection)"},
            {"429", "Developer Rate Limit Exceeded."}
        };

        public static string[,] LeagueResponceErrors =
        {
            {"400", "Bad Request."},
            {"401", "Unauthorized. Invalid API Key."},
            {"404", "League Not Found. No Ranked Statistics."},
            {"429", "Rate Limited Exceeded."},
            {"500", "Internal Server Error. Try Again Later."},
            {"503","Service Unavailable. Refer To 'https://developer.riotgames.com/status' For More Info."}
        };

        public static string[,] RankedStatsErrors =
        {
            {"400", "Bad Request."},
            {"401", "Unauthorized. Invalid API Key."},
            {"404", "Stats data Not Found. No Ranked Statistics."},
            {"429", "Rate Limited Exceeded."},
            {"500", "Internal Server Error. Try Again Later."},
            {"503","Service Unavailable. Refer To 'https://developer.riotgames.com/status' For More Info."}
        };

        private static WebClient webClient;
        private SummonerByName player;
        private CurrentGame game;
        private League league;
        private RankedStats[] rankedStatsResultsArray;



        public Form1()
        {
            InitializeComponent();

            // Set Defaults
            // Hide loading picture
            pictureBoxLoading.Hide();
            // Region
            comboBoxRegion.SelectedIndex = 5;
            // Adjust Form Size
            int clientWidth = this.ClientSize.Width;
            this.ClientSize = new Size(clientWidth, 120); //550
            // Hide all player stats,mm
            groupBoxEnemyPlayer1.Hide();
            groupBoxEnemyPlayer2.Hide();
            groupBoxEnemyPlayer3.Hide();
            groupBoxEnemyPlayer4.Hide();
            groupBoxEnemyPlayer5.Hide();

            //temp
            textBoxSummonerName.Text = "YourSummonerName";
            textBoxAPIKey.Text = apiKey;
            
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            pictureBoxLoading.Show();
            SetDefaultValuesSummonerByName();
        }

        private void buttonAllyTeam_Click(object sender, EventArgs e)
        {
            pictureBoxLoading.Show();
            searchEnemy = false;
            SetDefaultValuesSummonerByName();
        }

        private bool ValidateGuid()
        {
            try { Guid aG = new Guid(apiKey); }
            catch { return false; }

            return true;
        }

        private void SetDefaultValuesSummonerByName()
        {
            // Set Variable
            apiKey = textBoxAPIKey.Text;
            // Set Summoner Name
            summonerName = textBoxSummonerName.Text.ToLower();
            // Set Server
            server = ServerList[comboBoxRegion.SelectedIndex];
            // Set Platform
            platform = PlatformList[comboBoxRegion.SelectedIndex];

            if (!ValidateGuid())
                MessageBox.Show("Invalid Dev Key Format - Key Invalid!\nCopy key directly from Dev Dashboard. ");
            else
                SummonerByName();
        }

        private void SummonerByName()
        {
            // Local Variables
            bool isErrors = false;

            // Check summoner name for spaces, remove them
            // TODO: be sure this works for everyone
            if (summonerName.Contains(" "))
                summonerName = summonerName.Replace(" ", "");

            // Concatenate SummonerNameRequest URL elements
            string SummonerByNameRequestURL = "https://" + server + ".api.pvp.net/api/lol/" + server
                                              + "/v1.4/summoner/by-name/" + summonerName + "?api_key=" + apiKey;
            // New web client instance
            webClient = new WebClient();
            // Try getting summonerNameRequest results
            try
            {
                string summonerNameRequestResults = webClient.DownloadString(SummonerByNameRequestURL);

                // Dirty Regex Magic (Replace 1st occurrence of username with "Summoner" - table header)
                Regex regReplace = new Regex(summonerName);
                summonerNameRequestResults = regReplace.Replace(summonerNameRequestResults, "Summoner", 1);

                // Instantiate player
                player = JsonConvert.DeserializeObject<SummonerByName>(summonerNameRequestResults);
                summonerID = player.Summoner.Id.ToString();
                textBoxSummonerID.Text = summonerID;
            }
            // Return error reason based on error code
            catch (Exception ex)
            {
                // Remove letters from error, leaving error code (thank you stackoverflow <3)
                for (int i = 0; i < SummonerResponceErrors.Length/2; i++)
                {
                    if (ex.Message.Contains(SummonerResponceErrors[i,0]))
                    {
                        textBoxSummonerID.Text = "ERROR: " + SummonerResponceErrors[i, 0];
                        MessageBox.Show(SummonerResponceErrors[i,1]);
                        isErrors = true;
                        break;
                    }
                }
                if (!isErrors)
                {
                    MessageBox.Show(ex.Message);
                    isErrors = true;
                }
                // Hide Loading
                pictureBoxLoading.Hide();
            }
            finally
            {
                // Debug Name & ID test
                //MessageBox.Show("Summoner Name: " + player.Name +
                //                "\nSummoner ID: " + player.Id);

                // Request CurrentGameInfo if no errors exist
                if (!isErrors)
                    CurrentGameInfo();
            }
        }

        // Take Summoner ID and get currentgameinfo
        private void CurrentGameInfo()
        {
            // Local Variables
            bool isErrors = false;
            //webClient = new WebClient(); // Unsure if this is needed or not?
            string getSpectatorGameInfo = ".api.pvp.net/observer-mode/rest/consumer/getSpectatorGameInfo/";

            // Concatenate CurrentGameRequest URL elements
            string currentGameRequestURL = "https://" + server + getSpectatorGameInfo +
                platform + "/" + player.Summoner.Id + "?api_key=" + apiKey;

            // Try getting currentGameInfo Results
            try
            {
                string currentGameInfoRequestInfo = webClient.DownloadString(currentGameRequestURL);
                // Instantiate game
                game = JsonConvert.DeserializeObject<CurrentGame>(currentGameInfoRequestInfo);
            }
            catch (Exception ex)
            {
                // Check error code for error reasoning
                for (int i = 0; i < CurrentGameResponceErrors.Length/2; i++)
                {
                    if (ex.Message.Contains(CurrentGameResponceErrors[i, 0]))
                    {
                        MessageBox.Show(CurrentGameResponceErrors[i, 1]);
                        isErrors  = true;
                        break;
                    }
                }
                // If no error, but catch is thrown, show custom message
                if (!isErrors)
                {
                    MessageBox.Show(ex.Message);
                    isErrors = true;
                }
                // Hide Loading
                pictureBoxLoading.Hide();
            }
            finally
            {
                if (!isErrors)
                {
                    //next
                    CreateEnemyList();
                    SetSummonerNames();
                    SetSummonerSpells();
                    SetSummonerChampionIcon();
                    LeagueInfo();
                    GetRankedStats();
                }
            }
            

        }

        private void GetRankedStats()
        {
            // Local Variables (Auto-Grab current season)
            bool isErrors = false;
            string currentSeason = "SEASON" + DateTime.Now.Year;
            string[] rankedStatsRequestURLArray = new string[enemySummonerIdArray.Length];
            rankedStatsResultsArray = new RankedStats[enemySummonerIdArray.Length];

            try
            {
                for (int i = 0; i < enemySummonerIdArray.Length; i++)
                {
                    // Generate Unique URL's (of enemy team)
                    rankedStatsRequestURLArray[i] = "https://" + server + ".api.pvp.net/api/lol/" + server +
                                                    "/v1.3/stats/by-summoner/" + enemySummonerIdArray[i] +
                                                    "/ranked?season="
                                                    + currentSeason + "&api_key=" + apiKey;

                    // Make temp variable for each response - error :(
                    string tempRankedStatsRequestInfo = webClient.DownloadString(rankedStatsRequestURLArray[i]);

                    // Instantiate Each Player
                    rankedStatsResultsArray[i] =
                        JsonConvert.DeserializeObject<RankedStats>(tempRankedStatsRequestInfo);

                }
            }
            catch (Exception ex)
            {
                // Check error code for error reasoning
                for (int i = 0; i < RankedStatsErrors.Length / 2; i++)
                {
                    if (ex.Message.Contains(RankedStatsErrors[i, 0]))
                    {
                        MessageBox.Show(RankedStatsErrors[i, 1]);
                        isErrors = true;
                        break;
                    }
                }
                // If no error, but catch is thrown, show custom message
                if (!isErrors)
                {
                    MessageBox.Show(ex.Message);
                    isErrors = true;
                }

                // Hide Loading
                pictureBoxLoading.Hide();
            }
            finally
            {
                if (!isErrors)
                {
                    SetSummonerStats();
                    // Add champion masteries here

                    // Hide Loading
                    pictureBoxLoading.Hide();
                }
                
            }
        }

        private void GetChampMasteryStats()
        {
            
        }

        private void SetSummonerStats()
        {
            // Local Variables
            int enemyCount = enemySummonerNameArray.Count();
            string kdaLabel = "KDA: ";
            string championKDALabel = "Champion(KDA): ";
            string championWinRateLabel = "Champion(WR): ";
            string championGeneralLabel = "General(";
            string championChampionLabel = "Champion(";

            Stats[] overallRankedArray = new Stats[enemyCount];
            Stats[] individualRankedArray = new Stats[enemyCount];

            int[] numberIndividualChampionWinsArray = new int[enemyCount];
            int[] numberIndividualChampionLosesArray = new int[enemyCount];
            decimal[] averageIndividualChampionKillsArray = new decimal[enemyCount];
            decimal[] averageIndividualTotalAssistsArray = new decimal[enemyCount];
            decimal[] averageIndividualTotalDeathsArray = new decimal[enemyCount];

            int[] numberOverallChampionWinsArray = new int[enemyCount];
            int[] numberOverallChampionLosesArray = new int[enemyCount];
            decimal[] averageOverallChampionKillsArray = new decimal[enemyCount];
            decimal[] averageOverallTotalAssistsArray = new decimal[enemyCount];
            decimal[] averageOverallTotalDeathsArray = new decimal[enemyCount];


            // Set Default Values
            for (int i = 0; i < individualRankedArray.Length; i++)
            {
                individualRankedArray[i] = null;
            }
            

            // Grab overall & individual stats
            for (int i = 0; i < enemyCount; i++)
            {
                if (rankedStatsResultsArray.Length >= 1) // Atleast 1 enemy
                {
                    if (enemySummonerIdArray[i] == rankedStatsResultsArray[0].summonerId)
                    {
                        // Overall KDA
                        bool overallFound = false;
                        foreach (var champion in rankedStatsResultsArray[0].champions)
                        {
                            if (champion.id == 0)
                            {
                                overallRankedArray[i] = champion.stats;
                                overallFound = true;
                            }
                            if (overallFound) break;
                        }

                        
                        bool individualFound = false;
                        foreach (var champion in rankedStatsResultsArray[0].champions)
                        {
                            //ERROR: champ.id and saved ID's don't match
                            if (champion.id == enemySummonerChampionId[i])
                            {
                                individualRankedArray[i] = champion.stats;
                                individualFound = true;
                            }
                            if (individualFound) break;
                        }
                        
                    }
                }

                if (enemyCount >= 2)// Atleast 2 enemy
                {
                    if (enemySummonerIdArray[i] == rankedStatsResultsArray[1].summonerId)
                    {
                        // Overall KDA
                        bool overallFound = false;
                        foreach (var champion in rankedStatsResultsArray[1].champions)
                        {
                            if (champion.id == 0)
                            {
                                overallRankedArray[i] = champion.stats;
                                overallFound = true;
                            }
                            if (overallFound) break;
                        }


                        // Individual KDA
                        bool individualFound = false;
                        foreach (var champion in rankedStatsResultsArray[1].champions)
                        {
                            if (champion.id == enemySummonerChampionId[i])
                            {
                                individualRankedArray[i] = champion.stats;
                                individualFound = true;
                            }
                            if (individualFound) break;
                        }

                    }
                }

                if (enemyCount >= 3) // Atleast 3 enemy
                {
                    if (enemySummonerIdArray[i] == rankedStatsResultsArray[2].summonerId)
                    {
                        // Overall KDA
                        bool overallFound = false;
                        foreach (var champion in rankedStatsResultsArray[2].champions)
                        {
                            if (champion.id == 0)
                            {
                                overallRankedArray[i] = champion.stats;
                                overallFound = true;
                            }
                            if (overallFound) break;
                        }


                        // Individual KDA
                        bool individualFound = false;
                        foreach (var champion in rankedStatsResultsArray[2].champions)
                        {
                            if (champion.id == enemySummonerChampionId[i])
                            {
                                individualRankedArray[i] = champion.stats;
                                individualFound = true;
                            }
                            if (individualFound) break;
                        }

                    }
                }

                if (enemyCount >= 4) // Atleast 4 enemy
                {
                    if (enemySummonerIdArray[i] == rankedStatsResultsArray[3].summonerId)
                    {
                        // Overall KDA
                        bool overallFound = false;
                        foreach (var champion in rankedStatsResultsArray[3].champions)
                        {
                            if (champion.id == 0)
                            {
                                overallRankedArray[i] = champion.stats;
                                overallFound = true;
                            }
                            if (overallFound) break;
                        }


                        // Individual KDA
                        bool individualFound = false;
                        foreach (var champion in rankedStatsResultsArray[3].champions)
                        {
                            if (champion.id == enemySummonerChampionId[i])
                            {
                                individualRankedArray[i] = champion.stats;
                                individualFound = true;
                            }
                            if (individualFound) break;
                        }

                    }
                }
                if (enemyCount >= 5) // Atleast 5 enemy
                {
                    if (enemySummonerIdArray[i] == rankedStatsResultsArray[4].summonerId)
                    {
                        // Overall KDA
                        bool overallFound = false;
                        foreach (var champion in rankedStatsResultsArray[4].champions)
                        {
                            if (champion.id == 0)
                            {
                                overallRankedArray[i] = champion.stats;
                                overallFound = true;
                            }
                            if (overallFound) break;
                        }


                        // Individual KDA
                        bool individualFound = false;
                        foreach (var champion in rankedStatsResultsArray[4].champions)
                        {
                            if (champion.id == enemySummonerChampionId[i])
                            {
                                individualRankedArray[i] = champion.stats;
                                individualFound = true;
                            }
                            if (individualFound) break;
                        }

                    }
                }
            }

            // Calculate individual KDA & Win Rate
            // TODO: be sure it works no data?
            for (int i = 0; i < enemyCount; i++)
            {
                if (individualRankedArray[i] != null)
                {
                    numberIndividualChampionWinsArray[i] = individualRankedArray[i].totalSessionsWon;
                    numberIndividualChampionLosesArray[i] = individualRankedArray[i].totalSessionsLost;
                    averageIndividualChampionKillsArray[i] = (decimal)individualRankedArray[i].totalChampionKills /
                                                             individualRankedArray[i].totalSessionsPlayed;
                    averageIndividualTotalAssistsArray[i] = (decimal)individualRankedArray[i].totalAssists /
                                                            individualRankedArray[i].totalSessionsPlayed;
                    averageIndividualTotalDeathsArray[i] = (decimal)individualRankedArray[i].totalDeathsPerSession /
                                                           individualRankedArray[i].totalSessionsPlayed;
                }
            }

            // Calculate overall KDA & Win Rate
            for (int i = 0; i < enemyCount; i++)
            {
                numberOverallChampionWinsArray[i] = overallRankedArray[i].totalSessionsWon;
                numberOverallChampionLosesArray[i] = overallRankedArray[i].totalSessionsLost;
                averageOverallChampionKillsArray[i] = (decimal) overallRankedArray[i].totalChampionKills/
                                                      overallRankedArray[i].totalSessionsPlayed;
                averageOverallTotalAssistsArray[i] = (decimal) overallRankedArray[i].totalAssists/
                                                     overallRankedArray[i].totalSessionsPlayed;
                averageOverallTotalDeathsArray[i] = (decimal) overallRankedArray[i].totalDeathsPerSession/
                                                    overallRankedArray[i].totalSessionsPlayed;
            }

            //Set Individual & Overall KDA & Win Rate
            // TODO: check why returns N/A if no champion kills
            for (int i = 0; i < enemyCount; i++)
            {
                decimal temp = 0;
                if (enemySummonerNameArray[i] == linkLabelChamp1.Text)
                {
                    // Individual Champ WR
                    if (numberIndividualChampionWinsArray[i] == 0 && numberIndividualChampionLosesArray[i] == 0)
                    {
                        labelChamp1ChampionWR.Text = championWinRateLabel + "N/A";
                       
                    }
                    else
                    {
                        temp = ((decimal)numberIndividualChampionWinsArray[i] /
                               (numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i]));
                        if (temp == 0)
                            labelChamp1ChampionWR.Text = championWinRateLabel + "0%";
                        else
                            labelChamp1ChampionWR.Text = championWinRateLabel + temp.ToString("##.#%");
                    }
                    // Overall Player KDA
                    if (averageOverallChampionKillsArray[i] != 0 && 
                        averageOverallTotalAssistsArray[i] != 0 &&
                        averageOverallTotalDeathsArray[i] != 0)
                    {
                        labelChamp1KDA.Text = kdaLabel + Math.Round(averageOverallChampionKillsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalAssistsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalDeathsArray[i]).ToString("##.#");
                    }
                    else
                    {
                        labelChamp1KDA.Text = kdaLabel + "N/A";
                    }
                    // Champion KDA
                    if (averageIndividualChampionKillsArray[i] == 0 &&
                        averageIndividualTotalAssistsArray[i] == 0 &&
                        averageIndividualTotalDeathsArray[i] == 0)
                    {
                        labelChamp1ChampionKDA.Text = championKDALabel + "N/A";
                    }
                    else
                    {
                        labelChamp1ChampionKDA.Text = championKDALabel +
                                                      Math.Round(averageIndividualChampionKillsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalDeathsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalAssistsArray[i]).ToString("##.#");
                    }
                    // General Game Count
                    temp = numberOverallChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp1General.Text = championGeneralLabel + temp + ")";
                    // Champion Game Count
                    temp = numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp1Champion.Text = championChampionLabel + temp + ")";
                }

                if (enemySummonerNameArray[i] == linkLabelChamp2.Text)
                {
                    if (numberIndividualChampionWinsArray[i] == 0 && numberIndividualChampionLosesArray[i] == 0)
                    {
                        labelChamp2ChampionWR.Text = championWinRateLabel + "N/A";
                        
                    }
                    else
                    {
                        temp = ((decimal)numberIndividualChampionWinsArray[i] /
                                (numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i]));
                        if (temp == 0)
                            labelChamp2ChampionWR.Text = championWinRateLabel + "0%";
                        else
                            labelChamp2ChampionWR.Text = championWinRateLabel + temp.ToString("##.#%");
                    }
                    // Overall Player KDA
                    if (averageOverallChampionKillsArray[i] != 0 &&
                        averageOverallTotalAssistsArray[i] != 0 &&
                        averageOverallTotalDeathsArray[i] != 0)
                    {
                        labelChamp2KDA.Text = kdaLabel + Math.Round(averageOverallChampionKillsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalAssistsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalDeathsArray[i]).ToString("##.#");
                    }
                    else
                    {
                        labelChamp2KDA.Text = kdaLabel + "N/A";
                    }
                    // Champion KDA
                    if (averageIndividualChampionKillsArray[i] == 0 &&
                        averageIndividualTotalAssistsArray[i] == 0 &&
                        averageIndividualTotalDeathsArray[i] == 0)
                    {
                        labelChamp2ChampionKDA.Text = championKDALabel + "N/A";
                    }
                    else
                    {
                        labelChamp2ChampionKDA.Text = championKDALabel +
                                                      Math.Round(averageIndividualChampionKillsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalDeathsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalAssistsArray[i]).ToString("##.#");
                    }
                    // General Game Count
                    temp = numberOverallChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp2General.Text = championGeneralLabel + temp + ")";
                    // Champion Game Count
                    temp = numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp2Champion.Text = championChampionLabel + temp + ")";
                }

                if (enemySummonerNameArray[i] == linkLabelChamp3.Text)
                {
                    if (numberIndividualChampionWinsArray[i] == 0 && numberIndividualChampionLosesArray[i] == 0)
                    {
                        labelChamp3ChampionWR.Text = championWinRateLabel + "N/A";
                    }
                    else
                    {
                        temp = ((decimal)numberIndividualChampionWinsArray[i] /
                                (numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i]));
                        if (temp == 0)
                            labelChamp3ChampionWR.Text = championWinRateLabel + "0%";
                        else
                            labelChamp3ChampionWR.Text = championWinRateLabel + temp.ToString("##.#%");
                    }
                    // Overall Player KDA
                    if (averageOverallChampionKillsArray[i] != 0 &&
                        averageOverallTotalAssistsArray[i] != 0 &&
                        averageOverallTotalDeathsArray[i] != 0)
                    {
                        labelChamp3KDA.Text = kdaLabel + Math.Round(averageOverallChampionKillsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalAssistsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalDeathsArray[i]).ToString("##.#");
                    }
                    else
                    {
                        labelChamp3KDA.Text = kdaLabel + "N/A";
                    }
                    // Champion KDA
                    if (averageIndividualChampionKillsArray[i] == 0 &&
                        averageIndividualTotalAssistsArray[i] == 0 &&
                        averageIndividualTotalDeathsArray[i] == 0)
                    {
                        labelChamp3ChampionKDA.Text = championKDALabel + "N/A";
                    }
                    else
                    {
                        labelChamp3ChampionKDA.Text = championKDALabel +
                                                      Math.Round(averageIndividualChampionKillsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalDeathsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalAssistsArray[i]).ToString("##.#");
                    }
                    // General Game Count
                    temp = numberOverallChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp3General.Text = championGeneralLabel + temp + ")";
                    // Champion Game Count
                    temp = numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp3Champion.Text = championChampionLabel + temp + ")";
                }

                if (enemySummonerNameArray[i] == linkLabelChamp4.Text)
                {
                    if (numberIndividualChampionWinsArray[i] == 0 && numberIndividualChampionLosesArray[i] == 0)
                    {
                        labelChamp4ChampionWR.Text = championWinRateLabel + "N/A";
                        
                    }
                    else
                    {
                        temp = ((decimal)numberIndividualChampionWinsArray[i] /
                                (numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i]));
                        if (temp == 0)
                            labelChamp4ChampionWR.Text = championWinRateLabel + "0%";
                        else
                            labelChamp4ChampionWR.Text = championWinRateLabel + temp.ToString("##.#%");
                    }
                    // Overall Player KDA
                    if (averageOverallChampionKillsArray[i] != 0 &&
                        averageOverallTotalAssistsArray[i] != 0 &&
                        averageOverallTotalDeathsArray[i] != 0)
                    {
                        labelChamp4KDA.Text = kdaLabel + Math.Round(averageOverallChampionKillsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalAssistsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalDeathsArray[i]).ToString("##.#");
                    }
                    else
                    {
                        labelChamp4KDA.Text = kdaLabel + "N/A";
                    }
                    // Champion KDA
                    if (averageIndividualChampionKillsArray[i] == 0 &&
                        averageIndividualTotalAssistsArray[i] == 0 &&
                        averageIndividualTotalDeathsArray[i] == 0)
                    {
                        labelChamp4ChampionKDA.Text = championKDALabel + "N/A";
                    }
                    else
                    {
                        labelChamp4ChampionKDA.Text = championKDALabel +
                                                      Math.Round(averageIndividualChampionKillsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalDeathsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalAssistsArray[i]).ToString("##.#");
                    }
                    // General Game Count
                    temp = numberOverallChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp4General.Text = championGeneralLabel + temp + ")";
                    // Champion Game Count
                    temp = numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp4Champion.Text = championChampionLabel + temp + ")";
                }

                if (enemySummonerNameArray[i] == linkLabelChamp5.Text)
                {
                    // Individual Champion KDA
                    if (numberIndividualChampionWinsArray[i] == 0 && numberIndividualChampionLosesArray[i] == 0)
                    {
                        labelChamp5ChampionWR.Text = championWinRateLabel + "N/A";
                    }
                    else
                    {
                        // 0% does not print?
                        temp = ((decimal) numberIndividualChampionWinsArray[i]/
                                (numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i]));
                        if (temp == 0)
                            labelChamp5ChampionWR.Text = championWinRateLabel + "0%";
                        else
                            labelChamp5ChampionWR.Text = championWinRateLabel + temp.ToString("##.#%");
                        
                    }
                    // Overall Player KDA
                    if (averageOverallChampionKillsArray[i] != 0 &&
                        averageOverallTotalAssistsArray[i] != 0 &&
                        averageOverallTotalDeathsArray[i] != 0)
                    {
                        labelChamp5KDA.Text = kdaLabel + Math.Round(averageOverallChampionKillsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalAssistsArray[i]).ToString("##.#") + "/" +
                                              Math.Round(averageOverallTotalDeathsArray[i]).ToString("##.#");
                    }
                    else
                    {
                        labelChamp5KDA.Text = kdaLabel + "N/A";
                    }
                    // Champion KDA
                    if (averageIndividualChampionKillsArray[i] == 0 &&
                        averageIndividualTotalAssistsArray[i] == 0 &&
                        averageIndividualTotalDeathsArray[i] == 0)
                    {
                        labelChamp5ChampionKDA.Text = championKDALabel + "N/A";
                    }
                    else
                    {
                        labelChamp5ChampionKDA.Text = championKDALabel +
                                                      Math.Round(averageIndividualChampionKillsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalDeathsArray[i]).ToString("##.#") + "/" +
                                                      Math.Round(averageIndividualTotalAssistsArray[i]).ToString("##.#");
                    }
                    // General Game Count
                    temp = numberOverallChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp5General.Text = championGeneralLabel + temp + ")";
                    // Champion Game Count
                    temp = numberIndividualChampionWinsArray[i] + numberIndividualChampionLosesArray[i];
                    labelChamp5Champion.Text = championChampionLabel + temp + ")";
                }
            }

        }

        private void LeagueInfo()
        {
            // Local Variables
            bool isErrors = false;
            string leagueInfoString1 = "/v2.5/league/by-summoner/";
            string leagueInfoString2 = "/entry?api_key=";
            string summonerIdString = "";

            // Concatenate SummonerID List
            for (int i = 0; i < enemySummonerIdArray.Length; i++)
            {
                if (i != 0)
                    summonerIdString += ", ";

                summonerIdString += enemySummonerIdArray[i];
            }
            
            string leagueInfoRequestURL = "https://" + server + ".api.pvp.net/api/lol/" + server + leagueInfoString1 + summonerIdString + leagueInfoString2 +
                                   apiKey;

            try
            {
                string leagueRequestInfo = webClient.DownloadString(leagueInfoRequestURL);

                // Remove ID's from string, replace with "SummonerName"
                for (int i = 0; i < enemySummonerIdArray.Length; i++)
                {
                    if (leagueRequestInfo.Contains(enemySummonerIdArray[i].ToString()))
                        leagueRequestInfo = leagueRequestInfo.Replace(enemySummonerIdArray[i].ToString(), "SummonerName");
                }

                // Instantiate game
                league = JsonConvert.DeserializeObject<League>(leagueRequestInfo);

                
            }
            catch (Exception ex)
            {
                //TODO: Add errors for this API call
                // Check error code for error reasoning
                for (int i = 0; i < LeagueResponceErrors.Length / 2; i++)
                {
                    if (ex.Message.Contains(LeagueResponceErrors[i, 0]))
                    {
                        MessageBox.Show(LeagueResponceErrors[i, 1]);
                        isErrors = true;
                        break;
                    }
                }
                if (!isErrors)
                {
                    MessageBox.Show(ex.Message);
                    isErrors = true;
                }
                // Hide Loading
                pictureBoxLoading.Hide();
            }
            finally
            {
                if (!isErrors)
                    SetSummonerRank();
            }



        }

        private void CreateEnemyList()
        {
            // Local Variables
            int blueCount = 0;
            int redCount = 0;
            int teamMemberCount = 0;

            // Find Team Ids
            for (int i = 0; i < game.Participants.Count; i++)
            {
                if (game.Participants[i].SummonerId.ToString() == summonerID)
                {
                    if (searchEnemy) // Enemy
                    {
                        var teamId = game.Participants[i].TeamId;

                        if (teamId == 100) // Summoner is on Blue side
                            enemyTeamID = 200;
                        else if (teamId == 200)// Summoner is on Red side
                            enemyTeamID = 100;
                    }
                    else if (!searchEnemy)
                    {
                        var teamId = game.Participants[i].TeamId;

                        if (teamId == 100) // Summoner is on Blue side
                            enemyTeamID = 100;
                        else if (teamId == 200)// Summoner is on Red side
                            enemyTeamID = 200;
                    }
                }

                if (game.Participants[i].TeamId == 100) // blue
                    blueCount++;
                else if (game.Participants[i].TeamId == 200) // red
                    redCount++;
            }

            // Create enemySummoner Arrays
            // Enemy is blue
            if (enemyTeamID == 100) 
            {
                enemySummonerNameArray = new string[blueCount];
                enemySummonerIdArray = new int[blueCount];
                enemySummonerSpellId1 = new int[blueCount];
                enemySummonerSpellId2 = new int[blueCount];
                enemySummonerChampionId = new int[blueCount];
            }
            // Enemy is red
            else if (enemyTeamID == 200) 
            {
                enemySummonerNameArray = new string[redCount];
                enemySummonerIdArray = new int[redCount];
                enemySummonerSpellId1 = new int[redCount];
                enemySummonerSpellId2 = new int[redCount];
                enemySummonerChampionId = new int[redCount];
            }

            // Fill enemySummoner Arrays
            for (int i = 0; i < game.Participants.Count; i++)
            {
                if (game.Participants[i].TeamId == enemyTeamID)
                {
                    
                    enemySummonerNameArray[teamMemberCount] = game.Participants[i].SummonerName;
                    enemySummonerIdArray[teamMemberCount] = game.Participants[i].SummonerId;
                    enemySummonerSpellId1[teamMemberCount] = game.Participants[i].Spell1Id;
                    enemySummonerSpellId2[teamMemberCount] = game.Participants[i].Spell2Id;
                    enemySummonerChampionId[teamMemberCount] = game.Participants[i].ChampionId;
                    teamMemberCount++;
                }
            }

            // Change Window Size according to players
            int newClientHeight = 120 + (enemySummonerNameArray.Length*95);
            ClientSize = new Size(ClientSize.Width, newClientHeight);

            switch (enemySummonerNameArray.Length)
            {
                case 1: groupBoxEnemyPlayer1.Show();
                    break;
                case 2: groupBoxEnemyPlayer1.Show();
                    groupBoxEnemyPlayer2.Show();
                    break;
                case 3: groupBoxEnemyPlayer1.Show();
                    groupBoxEnemyPlayer2.Show();
                    groupBoxEnemyPlayer3.Show();
                    break;
                case 4: groupBoxEnemyPlayer1.Show();
                    groupBoxEnemyPlayer2.Show();
                    groupBoxEnemyPlayer3.Show();
                    groupBoxEnemyPlayer4.Show();
                    break;
                case 5: groupBoxEnemyPlayer1.Show();
                    groupBoxEnemyPlayer2.Show();
                    groupBoxEnemyPlayer3.Show();
                    groupBoxEnemyPlayer4.Show();
                    groupBoxEnemyPlayer5.Show();
                    break;
            }

        }

        private void SetSummonerNames()
        {
            // 1 Enemy Players
            if (enemySummonerNameArray.Length >= 1)
                linkLabelChamp1.Text = enemySummonerNameArray[0];
            // 2 Enemy Players
            if (enemySummonerNameArray.Length >= 2)
                linkLabelChamp2.Text = enemySummonerNameArray[1];
            // 3 Enemy Players
            if (enemySummonerNameArray.Length >= 3)
                linkLabelChamp3.Text = enemySummonerNameArray[2];
            // 4 Enemy Players
            if (enemySummonerNameArray.Length >= 4)
                linkLabelChamp4.Text = enemySummonerNameArray[3];
            // 5 Enemy Players
            if (enemySummonerNameArray.Length >= 5)
                linkLabelChamp5.Text = enemySummonerNameArray[4];
            
        }

        private void SetSummonerSpells()
        {
            // 1 Enemy Players
            if (enemySummonerNameArray.Length >= 1)
            {
                pictureBoxChamp1Spell1.Image = GetSummonerSpell(enemySummonerSpellId1[0]);
                pictureBoxChamp1Spell1.Tag = enemySummonerSpellId1[0];
                pictureBoxChamp1Spell2.Image = GetSummonerSpell(enemySummonerSpellId2[0]);
                pictureBoxChamp1Spell2.Tag = enemySummonerSpellId2[0];
            }
            // 2 Enemy Players
            if (enemySummonerNameArray.Length >= 2)
            {
                pictureBoxChamp2Spell1.Image = GetSummonerSpell(enemySummonerSpellId1[1]);
                pictureBoxChamp2Spell1.Tag = enemySummonerSpellId1[1];
                pictureBoxChamp2Spell2.Image = GetSummonerSpell(enemySummonerSpellId2[1]);
                pictureBoxChamp2Spell2.Tag = enemySummonerSpellId2[1];
            }
            // 3 Enemy Players
            if (enemySummonerNameArray.Length >= 3)
            {
                pictureBoxChamp3Spell1.Image = GetSummonerSpell(enemySummonerSpellId1[2]);
                pictureBoxChamp3Spell1.Tag = enemySummonerSpellId1[2];
                pictureBoxChamp3Spell2.Image = GetSummonerSpell(enemySummonerSpellId2[2]);
                pictureBoxChamp3Spell2.Tag = enemySummonerSpellId2[2];
            }
            // 4 Enemy Players
            if (enemySummonerNameArray.Length >= 4)
            {
                pictureBoxChamp4Spell1.Image = GetSummonerSpell(enemySummonerSpellId1[3]);
                pictureBoxChamp4Spell1.Tag = enemySummonerSpellId1[3];
                pictureBoxChamp4Spell2.Image = GetSummonerSpell(enemySummonerSpellId2[3]);
                pictureBoxChamp4Spell2.Tag = enemySummonerSpellId2[3];
            }
            // 5 Enemy Players
            if (enemySummonerNameArray.Length >= 5)
            {
                pictureBoxChamp5Spell1.Image = GetSummonerSpell(enemySummonerSpellId1[4]);
                pictureBoxChamp5Spell1.Tag = enemySummonerSpellId1[4];
                pictureBoxChamp5Spell2.Image = GetSummonerSpell(enemySummonerSpellId2[4]);
                pictureBoxChamp5Spell2.Tag = enemySummonerSpellId2[4];
            }
            
        }

        private void SetSummonerChampionIcon()
        {
            // 1 Enemy Players
            if (enemySummonerNameArray.Length >= 1)
            {
                pictureBoxChamp1.Image = GetSummonerChampionIcon(enemySummonerChampionId[0]);
            }
                
            // 2 Enemy Players
            if (enemySummonerNameArray.Length >= 2)
            {
                pictureBoxChamp2.Image = GetSummonerChampionIcon(enemySummonerChampionId[1]);
            }
                
            // 3 Enemy Players
            if (enemySummonerNameArray.Length >= 3)
            {
                pictureBoxChamp3.Image = GetSummonerChampionIcon(enemySummonerChampionId[2]);
            }
                
            // 4 Enemy Players
            if (enemySummonerNameArray.Length >= 4)
            {
                pictureBoxChamp4.Image = GetSummonerChampionIcon(enemySummonerChampionId[3]);
            }
                
            // 5 Enemy Players
            if (enemySummonerNameArray.Length >= 5)
            {
                pictureBoxChamp5.Image = GetSummonerChampionIcon(enemySummonerChampionId[4]);
            }
                
            
            
        }

        private void SetSummonerRank()
        {
            // Local Variables
            string rankLabel = "Rank: ";
            string divisionLabel = "Division: ";
            string leaguepointsLabel = "LP: ";
            string recruitLabel = "Recruit: ";
            string veteranLabel = "Veteran: ";
            string streakLabel = "Streak: ";
            string winrateLabel = "Win-Rate: ";
            string winrateEndLabel = "%";
            string[] rankLabelArray = new string[enemySummonerNameArray.Length];
            string[] divisionLabelArray = new string[enemySummonerNameArray.Length];
            int[] leaguepointsLabelArray = new int[enemySummonerNameArray.Length];
            bool[] recruitLabelArray = new bool[enemySummonerNameArray.Length];
            bool[] veteranLabelArray = new bool[enemySummonerNameArray.Length];
            bool[] streakLabelArray = new bool[enemySummonerNameArray.Length];

            int[] winsLabelArray = new int[enemySummonerNameArray.Length];
            int[] losesLabelArray = new int[enemySummonerNameArray.Length];

            // Set Default Values
            for (int i = 0; i < rankLabelArray.Length; i++)
                rankLabelArray[i] = "N/A  ";
            for (int i = 0; i < divisionLabelArray.Length; i++)
                divisionLabelArray[i] = "N/A";
            for (int i = 0; i < leaguepointsLabelArray.Length; i++)
                leaguepointsLabelArray[i] = 0;
            for (int i = 0; i < recruitLabelArray.Length; i++)
                recruitLabelArray[i] = false;
            for (int i = 0; i < veteranLabelArray.Length; i++)
                veteranLabelArray[i] = false;
            for (int i = 0; i < streakLabelArray.Length; i++)
                streakLabelArray[i] = false;

            //Grab Rank & Divisions
            for (int i = 0; i < enemySummonerNameArray.Length; i++) // Menu
            {

                // 1st
                if (league.SummonerName.Count >= 1)
                {
                    if (enemySummonerNameArray[i] == league.SummonerName[0].entries[0].playerOrTeamName)
                    {
                        rankLabelArray[i] = league.SummonerName[0].tier;
                        divisionLabelArray[i] = league.SummonerName[0].entries[0].division;
                        leaguepointsLabelArray[i] = league.SummonerName[0].entries[0].leaguePoints;
                        winsLabelArray[i] = league.SummonerName[0].entries[0].wins;
                        losesLabelArray[i] = league.SummonerName[0].entries[0].losses;
                        recruitLabelArray[i] = league.SummonerName[0].entries[0].isFreshBlood;
                        veteranLabelArray[i] = league.SummonerName[0].entries[0].isVeteran;
                        streakLabelArray[i] = league.SummonerName[0].entries[0].isHotStreak;
                    }
                }

                // 2nd
                if (league.SummonerName.Count >= 2)
                {
                    if (enemySummonerNameArray[i] == league.SummonerName[1].entries[0].playerOrTeamName)
                    {
                        rankLabelArray[i] = league.SummonerName[1].tier;
                        divisionLabelArray[i] = league.SummonerName[1].entries[0].division;
                        leaguepointsLabelArray[i] = league.SummonerName[1].entries[0].leaguePoints;
                        winsLabelArray[i] = league.SummonerName[1].entries[0].wins;
                        losesLabelArray[i] = league.SummonerName[1].entries[0].losses;
                        recruitLabelArray[i] = league.SummonerName[1].entries[0].isFreshBlood;
                        veteranLabelArray[i] = league.SummonerName[1].entries[0].isVeteran;
                        streakLabelArray[i] = league.SummonerName[1].entries[0].isHotStreak;
                    } 
                }

                // 3rd
                if (league.SummonerName.Count >= 3)
                {
                    if (enemySummonerNameArray[i] == league.SummonerName[2].entries[0].playerOrTeamName)
                    {
                        rankLabelArray[i] = league.SummonerName[2].tier;
                        divisionLabelArray[i] = league.SummonerName[2].entries[0].division;
                        leaguepointsLabelArray[i] = league.SummonerName[2].entries[0].leaguePoints;
                        winsLabelArray[i] = league.SummonerName[2].entries[0].wins;
                        losesLabelArray[i] = league.SummonerName[2].entries[0].losses;
                        recruitLabelArray[i] = league.SummonerName[2].entries[0].isFreshBlood;
                        veteranLabelArray[i] = league.SummonerName[2].entries[0].isVeteran;
                        streakLabelArray[i] = league.SummonerName[2].entries[0].isHotStreak;
                    }
                }

                // 4th
                if (league.SummonerName.Count >= 4)
                {
                    if (enemySummonerNameArray[i] == league.SummonerName[3].entries[0].playerOrTeamName)
                    {
                        rankLabelArray[i] = league.SummonerName[3].tier;
                        divisionLabelArray[i] = league.SummonerName[3].entries[0].division;
                        leaguepointsLabelArray[i] = league.SummonerName[3].entries[0].leaguePoints;
                        winsLabelArray[i] = league.SummonerName[3].entries[0].wins;
                        losesLabelArray[i] = league.SummonerName[3].entries[0].losses;
                        recruitLabelArray[i] = league.SummonerName[3].entries[0].isFreshBlood;
                        veteranLabelArray[i] = league.SummonerName[3].entries[0].isVeteran;
                        streakLabelArray[i] = league.SummonerName[3].entries[0].isHotStreak;
                    }
                }

                // 5th
                if (league.SummonerName.Count >= 5)
                {
                    if (enemySummonerNameArray[i] == league.SummonerName[4].entries[0].playerOrTeamName)
                    {
                        rankLabelArray[i] = league.SummonerName[4].tier;
                        divisionLabelArray[i] = league.SummonerName[4].entries[0].division;
                        leaguepointsLabelArray[i] = league.SummonerName[4].entries[0].leaguePoints;
                        winsLabelArray[i] = league.SummonerName[4].entries[0].wins;
                        losesLabelArray[i] = league.SummonerName[4].entries[0].losses;
                        recruitLabelArray[i] = league.SummonerName[4].entries[0].isFreshBlood;
                        veteranLabelArray[i] = league.SummonerName[4].entries[0].isVeteran;
                        streakLabelArray[i] = league.SummonerName[4].entries[0].isHotStreak;
                    }
                }
                
                    
            }

            // Set Rank & Division Labels
            for (int i = 0; i < enemySummonerNameArray.Length; i++)
            {
                // Champ 1
                decimal tempWinRate = 0;
                if (enemySummonerNameArray[i] == linkLabelChamp1.Text)
                {
                    labelChamp1Rank.Text = rankLabel + rankLabelArray[i].Substring(0, 4);
                    pictureBoxChamp1Rank.Image = GetRankImage(rankLabelArray[i]);
                    pictureBoxChamp1Rank.Tag = rankLabelArray[i];
                    labelChamp1Division.Text = divisionLabel + divisionLabelArray[i];
                    labelChamp1LP.Text = leaguepointsLabel + leaguepointsLabelArray[i];
                    pictureBoxChamp1Recruit.Image = GetBoolImage(recruitLabelArray[i]);
                    pictureBoxChamp1Veteran.Image = GetBoolImage(veteranLabelArray[i]);
                    pictureBoxChamp1Streak.Image = GetBoolImage(streakLabelArray[i]);
                    // If no ranked games
                    if (winsLabelArray[i] == 0 && losesLabelArray[i] == 0)
                        labelChampion1WinRate.Text = winrateLabel + "N/A";
                    else
                    {
                        tempWinRate = ((winsLabelArray[i]) / (decimal)(winsLabelArray[i] + losesLabelArray[i])) * 100;
                        labelChampion1WinRate.Text = winrateLabel + tempWinRate.ToString("##") + winrateEndLabel;
                    }
                    
                }
                // Champ 2
                if (enemySummonerNameArray[i] == linkLabelChamp2.Text)
                {
                    labelChamp2Rank.Text = rankLabel + rankLabelArray[i].Substring(0, 4);
                    pictureBoxChamp2Rank.Image = GetRankImage(rankLabelArray[i]);
                    pictureBoxChamp2Rank.Tag = rankLabelArray[i];
                    labelChamp2Division.Text = divisionLabel + divisionLabelArray[i];
                    labelChamp2LP.Text = leaguepointsLabel + leaguepointsLabelArray[i];
                    pictureBoxChamp2Recruit.Image = GetBoolImage(recruitLabelArray[i]);
                    pictureBoxChamp2Veteran.Image = GetBoolImage(veteranLabelArray[i]);
                    pictureBoxChamp2Streak.Image = GetBoolImage(streakLabelArray[i]);
                    // If no ranked games
                    if (winsLabelArray[i] == 0 && losesLabelArray[i] == 0)
                        labelChampion2WinRate.Text = winrateLabel + "N/A";
                    else
                    {
                        tempWinRate = ((winsLabelArray[i]) / (decimal)(winsLabelArray[i] + losesLabelArray[i])) * 100;
                        labelChampion2WinRate.Text = winrateLabel + tempWinRate.ToString("##") + winrateEndLabel;
                    }
                }
                // Champ 3
                if (enemySummonerNameArray[i] == linkLabelChamp3.Text)
                {
                    labelChamp3Rank.Text = rankLabel + rankLabelArray[i].Substring(0, 4);
                    pictureBoxChamp3Rank.Image = GetRankImage(rankLabelArray[i]);
                    pictureBoxChamp3Rank.Tag = rankLabelArray[i];
                    labelChamp3Division.Text = divisionLabel + divisionLabelArray[i];
                    labelChamp3LP.Text = leaguepointsLabel + leaguepointsLabelArray[i];
                    pictureBoxChamp3Recruit.Image = GetBoolImage(recruitLabelArray[i]);
                    pictureBoxChamp3Veteran.Image = GetBoolImage(veteranLabelArray[i]);
                    pictureBoxChamp3Streak.Image = GetBoolImage(streakLabelArray[i]); ;
                    // If no ranked games
                    if (winsLabelArray[i] == 0 && losesLabelArray[i] == 0)
                        labelChampion3WinRate.Text = winrateLabel + "N/A";
                    else
                    {
                        tempWinRate = ((winsLabelArray[i]) / (decimal)(winsLabelArray[i] + losesLabelArray[i])) * 100;
                        labelChampion3WinRate.Text = winrateLabel + tempWinRate.ToString("##") + winrateEndLabel;
                    }
                }
                // Champ 4
                if (enemySummonerNameArray[i] == linkLabelChamp4.Text)
                {
                    labelChamp4Rank.Text = rankLabel + rankLabelArray[i].Substring(0, 4);
                    pictureBoxChamp4Rank.Image = GetRankImage(rankLabelArray[i]);
                    pictureBoxChamp4Rank.Tag = rankLabelArray[i];
                    labelChamp4Division.Text = divisionLabel + divisionLabelArray[i];
                    labelChamp4LP.Text = leaguepointsLabel + leaguepointsLabelArray[i];
                    pictureBoxChamp4Recruit.Image = GetBoolImage(recruitLabelArray[i]);
                    pictureBoxChamp4Veteran.Image = GetBoolImage(veteranLabelArray[i]);
                    pictureBoxChamp4Streak.Image = GetBoolImage(streakLabelArray[i]);
                    // If no ranked games
                    if (winsLabelArray[i] == 0 && losesLabelArray[i] == 0)
                        labelChampion4WinRate.Text = winrateLabel + "N/A";
                    else
                    {
                        tempWinRate = ((winsLabelArray[i]) / (decimal)(winsLabelArray[i] + losesLabelArray[i]) * 100);
                        labelChampion4WinRate.Text = winrateLabel + tempWinRate.ToString("##") + winrateEndLabel;
                    }
                }
                // Champ 5
                if (enemySummonerNameArray[i] == linkLabelChamp5.Text)
                {
                    labelChamp5Rank.Text = rankLabel + rankLabelArray[i].Substring(0, 4);
                    pictureBoxChamp5Rank.Image = GetRankImage(rankLabelArray[i]);
                    pictureBoxChamp5Rank.Tag = rankLabelArray[i];
                    labelChamp5Division.Text = divisionLabel + divisionLabelArray[i];
                    labelChamp5LP.Text = leaguepointsLabel + leaguepointsLabelArray[i];
                    pictureBoxChamp5Recruit.Image = GetBoolImage(recruitLabelArray[i]);
                    pictureBoxChamp5Veteran.Image = GetBoolImage(veteranLabelArray[i]);
                    pictureBoxChamp5Streak.Image = GetBoolImage(streakLabelArray[i]); ;
                    // If no ranked games
                    if (winsLabelArray[i] == 0 && losesLabelArray[i] == 0)
                        labelChampion5WinRate.Text = winrateLabel + "N/A";
                    else
                    {
                        tempWinRate = ((winsLabelArray[i]) / (decimal)(winsLabelArray[i] + losesLabelArray[i])) * 100;
                        labelChampion5WinRate.Text = winrateLabel + tempWinRate.ToString("##") + winrateEndLabel;
                    }
                }
            }


        }

        private Image GetRankImage(string rank)
        {
            Image rankImage = null;

            switch (rank)
            {
                case "BRONZE":
                    rankImage = Properties.Resources.bronze;
                    break;
                case "SILVER":
                    rankImage = Properties.Resources.silver;
                    break;
                case "GOLD":
                    rankImage = Properties.Resources.gold;
                    break;
                case "PLATINUM":
                    rankImage = Properties.Resources.platinum;
                    break;
                case "DIAMOND":
                    rankImage = Properties.Resources.diamond;
                    break;
                case "MASTER":
                    rankImage = Properties.Resources.master;
                    break;
                case "CHALLENGER":
                    rankImage = Properties.Resources.challenger;
                    break;
                default:
                    rankImage = Properties.Resources.provisional;
                    break;
            }
            return rankImage;
        }

        private Image GetBoolImage(bool status)
        {
            Image statusImage;

            switch (status)
            {
                case true:
                    statusImage = Properties.Resources._true;
                    break;
                case false:
                    statusImage = Properties.Resources._false;
                    break;
                default:
                    statusImage = Properties.Resources.unknown;
                    break;
            }
            return statusImage;
        }

        private Image GetSummonerChampionIcon(int champId)
        {
            Image championImage;

            switch (champId)
            {
                case 1:
                    championImage = Properties.Resources._1;
                    break;
                case 2:
                    championImage = Properties.Resources._2;
                    break;
                case 3:
                    championImage = Properties.Resources._3;
                    break;
                case 4:
                    championImage = Properties.Resources._4;
                    break;
                case 5:
                    championImage = Properties.Resources._5;
                    break;
                case 6:
                    championImage = Properties.Resources._6;
                    break;
                case 7:
                    championImage = Properties.Resources._7;
                    break;
                case 8:
                    championImage = Properties.Resources._8;
                    break;
                case 9:
                    championImage = Properties.Resources._9;
                    break;
                case 10:
                    championImage = Properties.Resources._10;
                    break;
                case 11:
                    championImage = Properties.Resources._11;
                    break;
                case 12:
                    championImage = Properties.Resources._12;
                    break;
                case 13:
                    championImage = Properties.Resources._13;
                    break;
                case 14:
                    championImage = Properties.Resources._14;
                    break;
                case 15:
                    championImage = Properties.Resources._15;
                    break;
                case 16:
                    championImage = Properties.Resources._16;
                    break;
                case 17:
                    championImage = Properties.Resources._17;
                    break;
                case 18:
                    championImage = Properties.Resources._18;
                    break;
                case 19:
                    championImage = Properties.Resources._19;
                    break;
                case 20:
                    championImage = Properties.Resources._20;
                    break;
                case 21:
                    championImage = Properties.Resources._21;
                    break;
                case 22:
                    championImage = Properties.Resources._22;
                    break;
                case 23:
                    championImage = Properties.Resources._23;
                    break;
                case 24:
                    championImage = Properties.Resources._24;
                    break;
                case 25:
                    championImage = Properties.Resources._25;
                    break;
                case 26:
                    championImage = Properties.Resources._26;
                    break;
                case 27:
                    championImage = Properties.Resources._27;
                    break;
                case 28:
                    championImage = Properties.Resources._28;
                    break;
                case 29:
                    championImage = Properties.Resources._29;
                    break;
                case 30:
                    championImage = Properties.Resources._30;
                    break;
                case 31:
                    championImage = Properties.Resources._31;
                    break;
                case 32:
                    championImage = Properties.Resources._32;
                    break;
                case 33:
                    championImage = Properties.Resources._33;
                    break;
                case 34:
                    championImage = Properties.Resources._34;
                    break;
                case 35:
                    championImage = Properties.Resources._35;
                    break;
                case 36:
                    championImage = Properties.Resources._36;
                    break;
                case 37:
                    championImage = Properties.Resources._37;
                    break;
                case 38:
                    championImage = Properties.Resources._38;
                    break;
                case 39:
                    championImage = Properties.Resources._39;
                    break;
                case 40:
                    championImage = Properties.Resources._40;
                    break;
                case 41:
                    championImage = Properties.Resources._41;
                    break;
                case 42:
                    championImage = Properties.Resources._42;
                    break;
                case 43:
                    championImage = Properties.Resources._43;
                    break;
                case 44:
                    championImage = Properties.Resources._44;
                    break;
                case 45:
                    championImage = Properties.Resources._45;
                    break;
                case 48:
                    championImage = Properties.Resources._48;
                    break;
                case 50:
                    championImage = Properties.Resources._50;
                    break;
                case 51:
                    championImage = Properties.Resources._51;
                    break;
                case 53:
                    championImage = Properties.Resources._53;
                    break;
                case 54:
                    championImage = Properties.Resources._54;
                    break;
                case 55:
                    championImage = Properties.Resources._55;
                    break;
                case 56:
                    championImage = Properties.Resources._56;
                    break;
                case 57:
                    championImage = Properties.Resources._57;
                    break;
                case 58:
                    championImage = Properties.Resources._58;
                    break;
                case 59:
                    championImage = Properties.Resources._59;
                    break;
                case 60:
                    championImage = Properties.Resources._60;
                    break;
                case 61:
                    championImage = Properties.Resources._61;
                    break;
                case 62:
                    championImage = Properties.Resources._62;
                    break;
                case 63:
                    championImage = Properties.Resources._63;
                    break;
                case 64:
                    championImage = Properties.Resources._64;
                    break;
                case 67:
                    championImage = Properties.Resources._67;
                    break;
                case 68:
                    championImage = Properties.Resources._68;
                    break;
                case 69:
                    championImage = Properties.Resources._69;
                    break;
                case 72:
                    championImage = Properties.Resources._72;
                    break;
                case 74:
                    championImage = Properties.Resources._74;
                    break;
                case 75:
                    championImage = Properties.Resources._75;
                    break;
                case 76:
                    championImage = Properties.Resources._76;
                    break;
                case 77:
                    championImage = Properties.Resources._77;
                    break;
                case 78:
                    championImage = Properties.Resources._78;
                    break;
                case 79:
                    championImage = Properties.Resources._79;
                    break;
                case 80:
                    championImage = Properties.Resources._80;
                    break;
                case 81:
                    championImage = Properties.Resources._81;
                    break;
                case 82:
                    championImage = Properties.Resources._82;
                    break;
                case 83:
                    championImage = Properties.Resources._83;
                    break;
                case 84:
                    championImage = Properties.Resources._84;
                    break;
                case 85:
                    championImage = Properties.Resources._85;
                    break;
                case 86:
                    championImage = Properties.Resources._86;
                    break;
                case 89:
                    championImage = Properties.Resources._89;
                    break;
                case 90:
                    championImage = Properties.Resources._90;
                    break;
                case 91:
                    championImage = Properties.Resources._91;
                    break;
                case 92:
                    championImage = Properties.Resources._92;
                    break;
                case 96:
                    championImage = Properties.Resources._96;
                    break;
                case 98:
                    championImage = Properties.Resources._98;
                    break;
                case 99:
                    championImage = Properties.Resources._99;
                    break;
                case 101:
                    championImage = Properties.Resources._101;
                    break;
                case 102:
                    championImage = Properties.Resources._102;
                    break;
                case 103:
                    championImage = Properties.Resources._103;
                    break;
                case 104:
                    championImage = Properties.Resources._104;
                    break;
                case 105:
                    championImage = Properties.Resources._105;
                    break;
                case 106:
                    championImage = Properties.Resources._106;
                    break;
                case 107:
                    championImage = Properties.Resources._107;
                    break;
                case 110:
                    championImage = Properties.Resources._110;
                    break;
                case 111:
                    championImage = Properties.Resources._111;
                    break;
                case 112:
                    championImage = Properties.Resources._112;
                    break;
                case 113:
                    championImage = Properties.Resources._113;
                    break;
                case 114:
                    championImage = Properties.Resources._114;
                    break;
                case 115:
                    championImage = Properties.Resources._115;
                    break;
                case 117:
                    championImage = Properties.Resources._117;
                    break;
                case 119:
                    championImage = Properties.Resources._119;
                    break;
                case 120:
                    championImage = Properties.Resources._120;
                    break;
                case 121:
                    championImage = Properties.Resources._121;
                    break;
                case 122:
                    championImage = Properties.Resources._122;
                    break;
                case 126:
                    championImage = Properties.Resources._126;
                    break;
                case 127:
                    championImage = Properties.Resources._127;
                    break;
                case 131:
                    championImage = Properties.Resources._131;
                    break;
                case 133:
                    championImage = Properties.Resources._133;
                    break;
                case 134:
                    championImage = Properties.Resources._134;
                    break;
                case 143:
                    championImage = Properties.Resources._143;
                    break;
                case 150:
                    championImage = Properties.Resources._150;
                    break;
                case 154:
                    championImage = Properties.Resources._154;
                    break;
                case 157:
                    championImage = Properties.Resources._157;
                    break;
                case 161:
                    championImage = Properties.Resources._161;
                    break;
                case 201:
                    championImage = Properties.Resources._201;
                    break;
                case 202:
                    championImage = Properties.Resources._202;
                    break;
                case 203:
                    championImage = Properties.Resources._203;
                    break;
                case 222:
                    championImage = Properties.Resources._222;
                    break;
                case 223:
                    championImage = Properties.Resources._223;
                    break;
                case 236:
                    championImage = Properties.Resources._236;
                    break;
                case 238:
                    championImage = Properties.Resources._238;
                    break;
                case 245:
                    championImage = Properties.Resources._245;
                    break;
                case 254:
                    championImage = Properties.Resources._254;
                    break;
                case 266:
                    championImage = Properties.Resources._266;
                    break;
                case 267:
                    championImage = Properties.Resources._267;
                    break;
                case 268:
                    championImage = Properties.Resources._268;
                    break;
                case 412:
                    championImage = Properties.Resources._412;
                    break;
                case 420:
                    championImage = Properties.Resources._420;
                    break;
                case 421:
                    championImage = Properties.Resources._421;
                    break;
                case 429:
                    championImage = Properties.Resources._429;
                    break;
                case 432:
                    championImage = Properties.Resources._432;
                    break;
                default:
                    championImage = Properties.Resources._0;
                    break;
            }

            return championImage;
        }

        private Image GetSummonerSpell(int spellId)
        {
            Image spellImage;

            switch (spellId)
            {
                case 1:
                    spellImage = Properties.Resources.ss1;
                    break;
                case 3:
                    spellImage = Properties.Resources.ss3;
                    break;
                case 4:
                    spellImage = Properties.Resources.ss4;
                    break;
                case 6:
                    spellImage = Properties.Resources.ss6;
                    break;
                case 7:
                    spellImage = Properties.Resources.ss7;
                    break;
                case 11:
                    spellImage = Properties.Resources.ss11;
                    break;
                case 12:
                    spellImage = Properties.Resources.ss12;
                    break;
                case 13:
                    spellImage = Properties.Resources.ss13;
                    break;
                case 14:
                    spellImage = Properties.Resources.ss14;
                    break;
                case 21:
                    spellImage = Properties.Resources.ss21;
                    break;
                case 30:
                    spellImage = Properties.Resources.ss30;
                    break;
                case 31:
                    spellImage = Properties.Resources.ss31;
                    break;
                case 32:
                    spellImage = Properties.Resources.ss31;
                    break;
                default:
                    spellImage = Properties.Resources.ss0;
                    break;
            }

            return spellImage;
        }

        private void linkLabelAPIKey_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://developer.riotgames.com/");
        }

        private void pictureBoxChamp1Rank_MouseHover(object sender, EventArgs e)
        {
            if (pictureBoxChamp1Rank.Image != null)
                toolTip1.SetToolTip(pictureBoxChamp1Rank, pictureBoxChamp1Rank.Tag.ToString()); 
            
        }

        private void pictureBoxChamp2Rank_MouseHover(object sender, EventArgs e)
        {
            if (pictureBoxChamp2Rank.Image != null)
                toolTip1.SetToolTip(pictureBoxChamp2Rank, pictureBoxChamp2Rank.Tag.ToString());
        }

        private void pictureBoxChamp3Rank_MouseHover(object sender, EventArgs e)
        {
            if (pictureBoxChamp3Rank.Image != null)
                toolTip1.SetToolTip(pictureBoxChamp3Rank, pictureBoxChamp3Rank.Tag.ToString());
        }

        private void pictureBoxChamp4Rank_MouseHover(object sender, EventArgs e)
        {
            if (pictureBoxChamp4Rank.Image != null)
                toolTip1.SetToolTip(pictureBoxChamp4Rank, pictureBoxChamp4Rank.Tag.ToString());
        }

        private void pictureBoxChamp5Rank_MouseHover(object sender, EventArgs e)
        {
            if (pictureBoxChamp5Rank.Image != null)
                toolTip1.SetToolTip(pictureBoxChamp5Rank, pictureBoxChamp5Rank.Tag.ToString());
        }

        private void textBoxSummonerName_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = buttonEnemyTeam;
        }

        

        

        


    }
}
