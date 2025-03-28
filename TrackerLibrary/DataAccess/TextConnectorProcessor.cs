﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using TrackerLibrary.Models;

// Load the text file
// Convert the text to List<PrizeModel>
// Find the max ID
// Add the new record with the new ID (max + 1 )
//Convert the prizes to list<string>
//Save the list<string> to the text file

namespace TrackerLibrary.DataAccess.TextHelpers
{
    public static class TextConnectorProcessor
    {
        public static string FullFilePath(this string fileName) //ex : prizeModels.csv
        {
            return $"{ConfigurationManager.AppSettings["filePath"]}\\{fileName}";
        }

        public static List<string> LoadFile(this string file)
        {
            if (!File.Exists(file))
            {
                return new List<string>();
            }

            return File.ReadAllLines(file).ToList();
        }
        
        public static List<PrizeModel> ConverToPrizeModels(this List<string> lines)
        {
            List<PrizeModel> output = new List<PrizeModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                PrizeModel p = new PrizeModel();
                p.Id = int.Parse(cols[0]);
                p.PlaceNumber = int.Parse(cols[1]);
                p.PlaceName = cols[2];
                p.PrizeAmount = decimal.Parse(cols[3]);
                p.PrizePercentage = double.Parse(cols[4]);

                output.Add(p);
            }
            return output;
        }

        public static List<PersonModel> ConverToPersonModels(this List<string> lines)
        {
            List<PersonModel> output = new List<PersonModel>();
            
            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                PersonModel p = new PersonModel();
                p.Id = int.Parse(cols[0]);
                p.FirstName = cols[1];
                p.LastName = cols[2];
                p.EmailAddress = cols[3];
                p.CellphoneNumber = cols[4];

                output.Add(p);
            }
            return output;
        }

        public static List<TeamModel> ConverToTeamsModels(this List<string> lines)
        {
            //id, team, name, list of ids separated by the pipe
            // 3, tim's team, 1|3|5
            List<TeamModel> output = new List<TeamModel>();
            List<PersonModel> people = GlobalConfig.PeopleFile.FullFilePath().LoadFile().ConverToPersonModels();
            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                TeamModel t = new TeamModel();
                t.Id = int.Parse(cols[0]);
                t.TeamName = cols[1];

                string[] personIds = cols[2].Split('|');

                foreach (string id in personIds)
                {
                    t.TeamMembers.Add(people.Where(x => x.Id == int.Parse(id)).First());

                }
                output.Add(t);

            }

            return output;


        }

        public static List<TournamentModel> ConverToTournamentModels (this List<string> lines)
        {
            //id, TournamentName, EntryFee, (id|id|id - EnteredTeams ) ,( id|id|id -Prizes ), (Round - id^id^id | id^id^id ^ id^id^id )
            List<TournamentModel> output = new List<TournamentModel>();
            List<TeamModel> teams = GlobalConfig.TeamsFile
                .FullFilePath()
                .LoadFile()
                .ConverToTeamsModels();
            List<PrizeModel> prize = GlobalConfig.PrizesFile
                .FullFilePath()
                .LoadFile()
                .ConverToPrizeModels();

            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConverToMatchupModels();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                TournamentModel tm = new TournamentModel();
                tm.Id = int.Parse(cols[0]);
                tm.TournamentName = cols[1];
                tm.EntryFee = decimal.Parse(cols[2]);
                
                string[] teamIds = cols[3].Split('|');

                foreach (string id in teamIds)
                {
                    tm.EnteredTeams.Add(teams.Where(x => x.Id == int.Parse(id)).First());

                }

                if (cols[4].Length > 0)
                {
                    string[] prizeids = cols[4].Split('|');

                    foreach (string id in prizeids)
                    {
                        tm.Prizes.Add(prize.Where(x => x.Id == int.Parse(id)).First());
                    } 
                }

                //Cature Rounds infomation
                string[] rounds = cols[5].Split('|');

                foreach (string round in rounds)
                {
                    string[] msText = round.Split('^');
                    List<MatchupModel> ms = new List<MatchupModel>();

                    foreach (string matchupModelTextId in msText)
                    {
                        ms.Add(matchups.Where(x => x.Id == int.Parse(matchupModelTextId)).First());
                    }
                    tm.Rounds.Add(ms);
                }

                output.Add(tm);

            }

            return output;

        }

        public static void SaveToTournamentFile(this List<TournamentModel> models)
        {
            List<string> lines = new List<string>();

            foreach (TournamentModel tm in models)
            {
                lines.Add($"{tm.Id},{tm.TournamentName},{tm.EntryFee},{ConvertTeamListToString(tm.EnteredTeams)},{ConvertPrizeListToString(tm.Prizes)},{ConvertRoundListToString(tm.Rounds)}");
            }

            File.WriteAllLines(GlobalConfig.TournamentFile.FullFilePath(), lines);
        }

        public static void SaveToPrizeFile(this List<PrizeModel> models)
        {
            List<string> lines = new List<string>();

            foreach (PrizeModel p in models)
            {
                lines.Add($"{p.Id}, {p.PlaceNumber},{p.PlaceName},{p.PrizeAmount},{p.PrizePercentage}");
            }

            File.WriteAllLines(GlobalConfig.PrizesFile.FullFilePath(), lines);
        }

        public static void SaveToPersonFile(this List<PersonModel> models)
        {
            List<string> lines = new List<string>();

            foreach (PersonModel p in models)
            {
                lines.Add($"{p.Id}, {p.FirstName},{p.LastName},{p.EmailAddress},{p.CellphoneNumber}");
            }

            File.WriteAllLines(GlobalConfig.PeopleFile.FullFilePath(), lines);
        }

        public static void SaveToTeamFile(this List<TeamModel> models)
        {
            List<string> lines = new List<string>();
            foreach (TeamModel t in models)
            {
                lines.Add($"{t.Id},{t.TeamName},{ConvertPeopleListToString(t.TeamMembers)}");
            }

            File.WriteAllLines(GlobalConfig.TeamsFile.FullFilePath(), lines);
        }

        public static void SaveRoundsToFile(this TournamentModel model)
        {
            //Loop through each Round
            //Loop through each Matchup
            //Get the id for the new matchup and save the record
            //Loop through each Entry, get the id, and svae it 

            foreach (List<MatchupModel> round in model.Rounds)
            {
                foreach (var matchup in round)
                {
                    //Load all of the matchups from file
                    //Get the top id and add one
                    //store the id
                    //save the matchup record
                    matchup.SaveMatchupToFile();

                   
                }
            }
        }

        public static List<MatchupEntryModel> ConvertToMatchupEntryModels(this List<string> lines)
        {   //id = 0, TeamCompeting = 1, Score =2 , ParentMatchup=3
            List<MatchupEntryModel> output = new List<MatchupEntryModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');


                MatchupEntryModel me = new MatchupEntryModel();
                me.Id = int.Parse(cols[0]);

                if (cols[1].Length == 0)
                {
                    me.TeamCompeting = null;
                }
                else
                {
                    me.TeamCompeting = LookupTeamById(int.Parse(cols[1]));
                }
                me.Score = double.Parse(cols[2]);

                int parentId = 0;
                if (int.TryParse(cols[3],out parentId))
                {
                    me.ParentMatchup = LookupMatchupById(int.Parse(cols[3]));
                }
                else
                {
                    me.ParentMatchup = null;
                }

                output.Add(me);
            }
            return output;
        }
        private static List<MatchupEntryModel> ConverStringToMatchupEntryModels(string input)
        {
            string[] ids = input.Split('|');

            List<MatchupEntryModel> output = new List<MatchupEntryModel>();
            List<string> entries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile();
            List<string> matchingEntries = new List<string>();
            foreach (string id in ids)
            {
                foreach (var entry in entries)
                {
                    string[] cols = entry.Split(',');
                    if (cols[0] == id)
                    {
                        matchingEntries.Add(entry);

                    }
                }
            }

            output = matchingEntries.ConvertToMatchupEntryModels();
            return output;
        }

        private static MatchupModel LookupMatchupById(int id)
        {
            List<string> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile();

            foreach (var matchup in matchups)
            {
                string[] cols = matchup.Split(',');
                if (cols[0] == id.ToString())
                {
                    List<string> matchingTeams = new List<string>();
                    matchingTeams.Add(matchup);
                    return matchingTeams.ConverToMatchupModels().First();
                }
            }
            return null;
        }
        private static TeamModel LookupTeamById(int id)
        {
            List<string> teams = GlobalConfig.TeamsFile.FullFilePath().LoadFile();

            foreach (var team in teams)
            {
                string[] cols = team.Split(',');
                if (cols[0] == id.ToString())
                {
                    List<string> matchingTeams = new List<string>();
                    matchingTeams.Add(team);
                    return matchingTeams.ConverToTeamsModels().First();
                }
            }
            return null;
        }

        public static List<MatchupModel> ConverToMatchupModels(this List<string> lines)
        {
            //id = 0, entries = 1 (pipe delimited by id ) , Winner=2 , matchupRound =3 
            List<MatchupModel> output = new List<MatchupModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                MatchupModel p = new MatchupModel();
                p.Id = int.Parse(cols[0]);
                p.Entries = ConverStringToMatchupEntryModels(cols[1]);
                if (cols[2].Length == 0)
                {
                    p.Winner = null;
                }
                else
                {
                    p.Winner = LookupTeamById(int.Parse(cols[2]));
                }
                p.MatchupRound = int.Parse(cols[3]);

                output.Add(p);
            }
            return output;
        }

        public static void SaveMatchupToFile(this MatchupModel matchup)
        {
            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConverToMatchupModels();

            int currentId = 1;

            if (matchups.Count > 0)
            {
                currentId = matchups.OrderByDescending(x => x.Id).First().Id + 1;
            }
            matchup.Id = currentId;

            matchups.Add(matchup);

            foreach (MatchupEntryModel enrty in matchup.Entries)
            {
                enrty.SaveEntryToFile();
            }

            //save to file
            List<string> lines = new List<string>();

            foreach (MatchupModel m in matchups)
            {
                string Winner = "";
                if (m.Winner != null)
                {
                    Winner = m.Winner.Id.ToString();
                }
                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries) },{Winner},{m.MatchupRound}");
            }
            File.WriteAllLines(GlobalConfig.MatchupFile.FullFilePath(), lines);
        }

        public static void UpdateMatchupToFile(this MatchupModel matchup)
        {
            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConverToMatchupModels();

            MatchupModel oldMatchup = new MatchupModel();
            foreach (MatchupModel m in matchups)
            {
                if(m.Id == matchup.Id)
                {
                    oldMatchup = m;
                }
            }
            matchups.Remove(oldMatchup);
            matchups.Add(matchup);

            foreach (MatchupEntryModel enrty in matchup.Entries)
            {
                enrty.UpdateEntryToFile();
            }

            //save to file
            List<string> lines = new List<string>();

            foreach (MatchupModel m in matchups)
            {
                string Winner = "";
                if (m.Winner != null)
                {
                    Winner = m.Winner.Id.ToString();
                }
                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries)},{Winner},{m.MatchupRound}");
            }
            File.WriteAllLines(GlobalConfig.MatchupFile.FullFilePath(), lines);
        }

        public static void SaveEntryToFile(this MatchupEntryModel entry)
        {
            List<MatchupEntryModel> entries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();

            int currentId = 1;

            if (entries.Count > 0)
            {
                currentId = entries.OrderByDescending(x => x.Id).First().Id + 1;
            }
            entry.Id = currentId;
            entries.Add(entry);

            List<string> lines = new List<string>();
            foreach (MatchupEntryModel e in entries)
            {
                string parent = "";
                if(e.ParentMatchup != null)
                {
                    parent = e.ParentMatchup.Id.ToString();
                }

                string teamCompeting = "";
                if (e.TeamCompeting != null)
                {
                    teamCompeting = e.TeamCompeting.Id.ToString();
                }
                lines.Add($"{e.Id},{teamCompeting},{e.Score},{parent}");
            }

            File.WriteAllLines(GlobalConfig.MatchupEntriesFile.FullFilePath(), lines);

        }

        public static void UpdateEntryToFile(this MatchupEntryModel entry)
        {
            List<MatchupEntryModel> entries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();

            MatchupEntryModel oldEntry = new MatchupEntryModel();

            foreach (MatchupEntryModel e in entries)
            {
                if(e.Id == entry.Id)
                {
                    oldEntry = e;
                }
            }
            entries.Remove(oldEntry);
            entries.Add(entry);

            List<string> lines = new List<string>();
            foreach (MatchupEntryModel e in entries)
            {
                string parent = "";
                if (e.ParentMatchup != null)
                {
                    parent = e.ParentMatchup.Id.ToString();
                }

                string teamCompeting = "";
                if (e.TeamCompeting != null)
                {
                    teamCompeting = e.TeamCompeting.Id.ToString();
                }
                lines.Add($"{e.Id},{teamCompeting},{e.Score},{parent}");
            }

            File.WriteAllLines(GlobalConfig.MatchupEntriesFile.FullFilePath(), lines);

        }
        private static string ConvertPeopleListToString(List<PersonModel> people) 
        {

            string output = "";

            if (people.Count == 0)
            {
                return "";
            }

            foreach (PersonModel p in people)
            {
                output += $"{p.Id}|";
            }
           
            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertTeamListToString(List<TeamModel> teams)
        {

            string output = "";

            if (teams.Count == 0)
            {
                return "";
            }

            foreach (TeamModel p in teams)
            {
                output += $"{p.Id}|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertMatchupEntryListToString(List<MatchupEntryModel> entries)
        {

            string output = "";

            if (entries.Count == 0)
            {
                return "";
            }

            foreach (MatchupEntryModel e in entries)
            {
                output += $"{e.Id}|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertPrizeListToString(List<PrizeModel> Prizes)
        {

            string output = "";

            if (Prizes.Count == 0)
            {
                return "";
            }

            foreach (PrizeModel p in Prizes)
            {
                output += $"{p.Id}|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertRoundListToString(List<List<MatchupModel>> Rounds)
        {

            string output = "";

            if (Rounds.Count == 0)
            {
                return "";
            }

            foreach (List<MatchupModel> r in Rounds)
            {
                output += $"{ConvertMatchupToString(r)}|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertMatchupToString(List<MatchupModel> Matchups)
        {
            string output = "";

            if (Matchups.Count == 0)
            {
                return "";
            }

            foreach (MatchupModel m in Matchups)
            {
                output += $"{m.Id}^";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }
    }
}
