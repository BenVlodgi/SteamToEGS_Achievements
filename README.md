# SteamToEGS Achievements

Processes copy values from Steam's localization file, and put them into EGS bulk import format. 
Icons are copied from the locale listed as default. 
If there is no default locale, it is copied from the language listed as DefaultLanguage.

In practice, run the tool to generate the csv. Fill in the proper icons in the "default" locale, then run it again with all languages so they all have the icons copied.

### Usage: 
- Name your keys file `in-keys.vdf`
  Example of what is in this file.
```
"keys"
{
"NEW_ACHIEVEMENT_1_0"	"Firepipe"
"NEW_ACHIEVEMENT_1_1"	"The_Blues"
"NEW_ACHIEVEMENT_1_2"	"Speedx2"
}
```
- Download your localizations from steam and name the file `in-loc_all.vdf`
 ![image](https://user-images.githubusercontent.com/1462374/165829444-04d06438-76c3-4c94-9832-8ed9654c46c5.png)
- You can now run the program which will generate `in-achievementLocalizations.csv` and `out-achievementLocalizations.csv`
 
