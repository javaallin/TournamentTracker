using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TrackerLibrary.Models
{
    public class MatchupModel
    {
        /// <summary>
        /// branch Test
        /// </summary>
        public int Id { get; set; } 
        public List<MatchupEntryModel> Entries { get; set; } = new List<MatchupEntryModel>();
        
        
        public int WinnerId {  get; set; }

        public TeamModel Winner { get; set; }

        public int MatchupRound { get; set; }

        public string DIsPlayName
        {
            get
            {
                string output = "";
                foreach (MatchupEntryModel me in Entries)
                {
                    if (me.TeamCompeting != null)
                    {
                        if (output.Length == 0)
                        {
                            output = me.TeamCompeting.TeamName;
                        }
                        else
                        {
                            output += $" vs. {me.TeamCompeting.TeamName}";
                        }
                    }
                    else
                    {
                        output = "Matchup Not Yet Determined";
                        break;
                    }

                }
                return output;
            }
        }
    }
}
