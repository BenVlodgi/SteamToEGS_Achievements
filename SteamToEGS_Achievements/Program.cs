using System;
using System.IO;
using VMFParser;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace SteamToEGS_Achievements
{
    
    public class EGSLocalization
    {
        public string Name;
        public string Locale;
        public string LockedTitle;
        public string LockedDescription;
        public string UnlockedTitle;
        public string UnlockedDescription;
        public string FlavorText;
        public string LockedIcon;
        public string UnlockedIcon;

        public EGSLocalization() { }

        public EGSLocalization(string[] fields)
        {
            Name = fields[0].Trim('"');
            Locale = fields[1].Trim('"'); 
            LockedTitle = fields[2].Trim('"'); 
            LockedDescription = fields[3].Trim('"');
            UnlockedTitle = fields[4].Trim('"');
            UnlockedDescription = fields[5].Trim('"');
            FlavorText = fields[6].Trim('"');
            LockedIcon = fields[7].Trim('"');
            UnlockedIcon = fields[8].Trim('"');
        }


        public override string ToString()
        {
            return string.Join(",", 
                new string[] { Name, Locale, LockedTitle, LockedDescription, UnlockedTitle, UnlockedDescription, FlavorText, LockedIcon, UnlockedIcon }
                .Select(element => $"\"{element}\"")
                );
        }
    }

    class Program
    {
        static Dictionary<string, string> LanguageConversion = new Dictionary<string, string>()
        {
            {"english","en-US"},
            {"french","fr"},
            {"korean","ko"},
            {"schinese","zh-Hans"},
            {"tchinese","zh-Hant"},
            {"japanese","ja"},
            {"portuguese","pt-BR"},
            {"finnish","fi"},
            //{"hungarian",""}, //Not in EGS
            {"turkish","tr"},
        };


        static void Main(string[] args)
        {
            string KeysVDFPath = "in-keys.vdf";
            string SteamVDFLanguagesPath = "in-loc_all.vdf";
            string AchievementLocalizationsPath = "in-achievementLocalizations.csv";
            string NewAchievementLocalizationsPath = "out-achievementLocalizations.csv";
            bool DoNotOverrideOrUpdateExistingLocalizations = false;

            var keysVDF = new VMF(File.ReadAllLines(KeysVDFPath))
                .Body.Where(b=>b.Name=="keys").VBlock();

            var steamVDFLanguages = new VMF(File.ReadAllLines(SteamVDFLanguagesPath))
                .Body.Where(b => b.Name == "lang").VBlock();

            var achievementLocalizations = new List<EGSLocalization>();
            TextFieldParser parser = new TextFieldParser(AchievementLocalizationsPath) 
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = new string[]{ "," } 
            };
            while (!parser.EndOfData)
            {
                achievementLocalizations.Add(new EGSLocalization(parser.ReadFields()));
            }

            // This way didn't account for commas inside fields
            //var achievementLocalizations = File.ReadAllLines(AchievementLocalizationsPath)
            //    .Select(line => line.Split(","))
            //    .Where(array => array.Length == 9)
            //    .Select(array => new EGSLocalization(array))
            //    .ToList();
            
            

            // For each achievement
            foreach (var key in keysVDF.Body)
            {
                var achievementSteamKey = key.Name;
                var achievementGameKey = (key as VProperty).Value;

                // find default values from existing Localization.
                var defaultLocalalization = achievementLocalizations.Where(loc => loc.Name == achievementGameKey && loc.Locale == "default").FirstOrDefault();
                string defaultLockedIcon = defaultLocalalization?.LockedIcon ?? "";
                string defaultUnlockedIcon = defaultLocalalization?.UnlockedIcon ?? "";

                // For each language
                foreach (var langSteam in LanguageConversion.Keys)
                {
                    var langEGS = LanguageConversion[langSteam];

                    var langTokensBlock = steamVDFLanguages
                        .Body.Where(b => b.Name == langSteam).VBlock()
                        .Body.Where(b => b.Name == "Tokens").VBlock();

                    var SteamNameToken = langTokensBlock.Body.Where(token => token.Name == $"{achievementSteamKey}_NAME").VProperty();
                    var SteamDescriptionToken = langTokensBlock.Body.Where(token => token.Name == $"{achievementSteamKey}_DESC").VProperty();
                    
                    // if these tokens exist, lets add or update this
                    if(!string.IsNullOrEmpty(SteamNameToken?.Value ?? "") && !string.IsNullOrEmpty(SteamDescriptionToken?.Value ?? ""))
                    {
                        var existingLocalization = achievementLocalizations.Where(loc => loc.Name == achievementGameKey && loc.Locale == langEGS).FirstOrDefault();
                        


                        if (!DoNotOverrideOrUpdateExistingLocalizations && existingLocalization != null)
                        {
                            existingLocalization.LockedTitle = existingLocalization.UnlockedTitle = SteamNameToken.Value;
                            existingLocalization.LockedDescription = existingLocalization.UnlockedDescription = SteamDescriptionToken.Value;
                        }
                        else
                        {
                            achievementLocalizations.Add(new EGSLocalization() { Name = achievementGameKey, Locale = langEGS, LockedTitle = SteamNameToken.Value, LockedDescription = SteamDescriptionToken.Value, UnlockedTitle = SteamNameToken.Value, UnlockedDescription = SteamDescriptionToken.Value, FlavorText = "", LockedIcon = defaultLockedIcon, UnlockedIcon = defaultUnlockedIcon, });
                        }
                    }
                }
            }

            File.WriteAllLines(NewAchievementLocalizationsPath, achievementLocalizations
                .Select(loc => loc.ToString()));
        }
    }
}
