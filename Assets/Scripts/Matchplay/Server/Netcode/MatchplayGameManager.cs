using System.Collections.Generic;
using Unity.Netcode;


namespace Matchplay.Server
{
    public enum TeamName 
    {
        Red,
        Blue,
        Green,
        Yellow
    } 
    
    public class Team
    {
        private List<Matchplayer> Players;
        
        public Team()
        {
            Players = new List<Matchplayer>();
        }
    
        public void AddPlayer(Matchplayer player)
        {
            Players.Add(player);
        }
    }

    public class Match
    {
        public Dictionary<TeamName, Team> Teams;

        public Match(TeamName teamName, Team team)
        {
            
        }
        
    }
    public class MatchplayGameManager : NetworkBehaviour
    {
        
    }
}