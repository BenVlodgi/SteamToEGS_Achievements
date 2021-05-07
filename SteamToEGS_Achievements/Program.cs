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

        public EGSLocalization Copy()
        {
            return new EGSLocalization() 
            { 
                Name = Name,
                Locale = Locale,
                LockedTitle = LockedTitle,
                LockedDescription = LockedDescription,
                UnlockedTitle = UnlockedTitle,
                UnlockedDescription = UnlockedDescription,
                FlavorText = FlavorText,
                LockedIcon = LockedIcon,
                UnlockedIcon = UnlockedIcon,
            };
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

        /// <summary>
        /// Processes copy values from Steam's localization file, and put them into EGS bulk import format. 
        /// Icons are copied from the locale listed as default. 
        /// If there is no default locale, it is copied from the language listed as DefaultLanguage.
        /// 
        /// In practice, run the tool to generate the csv. Fill in the proper icons in the "default" locale, then run it again with all languages so they all have the icons copied.
        /// </summary>
        static void Main(string[] args)
        {
            string KeysVDFPath = "in-keys.vdf";
            string SteamVDFLanguagesPath = "in-loc_all.vdf";
            string AchievementLocalizationsPath = "in-achievementLocalizations.csv";
            string NewAchievementLocalizationsPath = "out-achievementLocalizations.csv";
            bool DoNotOverrideOrUpdateExistingLocalizations = false;
            string DefaultLanguage = "en-US"; // If there is no default entry or if there is no "in-achievementLocalizations.csv" then generate new entry from this specified language.
            bool OverwriteIconsInExistingLanguages = true;

            if (!File.Exists(KeysVDFPath))
            {
                Console.WriteLine($"Path to keys is invalid ({KeysVDFPath}).");
                return;
            }
            var keysVDF = new VMF(File.ReadAllLines(KeysVDFPath))
                .Body.Where(b=>b.Name=="keys").VBlock();


            if (!File.Exists(SteamVDFLanguagesPath))
            {
                Console.WriteLine($"Path to Steam Localization VDF is invalid ({SteamVDFLanguagesPath}).");
                return;
            }
            var steamVDFLanguages = new VMF(File.ReadAllLines(SteamVDFLanguagesPath))
                .Body.Where(b => b.Name == "lang").VBlock();

            
            var achievementLocalizations = new List<EGSLocalization>();
            if (File.Exists(AchievementLocalizationsPath))
            {
                TextFieldParser parser = new TextFieldParser(AchievementLocalizationsPath)
                {
                    TextFieldType = FieldType.Delimited,
                    Delimiters = new string[] { "," }
                };
                while (!parser.EndOfData)
                {
                    achievementLocalizations.Add(new EGSLocalization(parser.ReadFields()));
                }
            }
            else
            {
                Console.WriteLine($"Path to existing Achievement Localization CSV is invalid ({AchievementLocalizationsPath}). But we can still continue.");
            }

            // For each achievement
            foreach (var key in keysVDF.Body)
            {
                var achievementSteamKey = key.Name;
                var achievementGameKey = (key as VProperty).Value;

                // find default values from existing Localization.
                var defaultLocalalization = achievementLocalizations.Where(loc => loc.Name == achievementGameKey && loc.Locale == "default").FirstOrDefault();

                // Get the icon strings from the default entry, to copy into new entries
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
                            if(OverwriteIconsInExistingLanguages && defaultLocalalization is not null)
                            {
                                existingLocalization.LockedIcon = defaultLockedIcon;
                                existingLocalization.UnlockedIcon = defaultUnlockedIcon;
                            }
                        }
                        else
                        {
                            achievementLocalizations.Add(new EGSLocalization() { Name = achievementGameKey, Locale = langEGS, LockedTitle = SteamNameToken.Value, LockedDescription = SteamDescriptionToken.Value, UnlockedTitle = SteamNameToken.Value, UnlockedDescription = SteamDescriptionToken.Value, FlavorText = "", LockedIcon = defaultLockedIcon, UnlockedIcon = defaultUnlockedIcon, });
                        }
                    }
                }

                // If there is no default value, so we'll create one if we can
                if (defaultLocalalization is null && !string.IsNullOrEmpty(DefaultLanguage))
                {
                    // Look up the default language entry for this achievement 
                    var defaultLocalalizationFromLanguage = achievementLocalizations.Where(loc => loc.Name == achievementGameKey && loc.Locale == DefaultLanguage).FirstOrDefault();

                    // If it exists, lets copy it to "Default" locale
                    if (defaultLocalalizationFromLanguage is not null)
                    {
                        defaultLocalalization = defaultLocalalizationFromLanguage.Copy();
                        defaultLocalalization.Locale = "default";
                        achievementLocalizations.Add(defaultLocalalization);
                    }
                }
            }

            File.WriteAllLines(NewAchievementLocalizationsPath, achievementLocalizations
                .Select(loc => loc.ToString()));
        }
    }
}
