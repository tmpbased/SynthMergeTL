using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System.Collections.Generic;

namespace MergeTL
{
    public class Configuration
    {
        public Configuration_Translation Translation { get; set; } = new Configuration_Translation();
    }

    public enum Encoding
    {
        none,
        _932,
        _1250,
        _1251,
        _1252,
        _1253,
        _1254,
        _1256,
        _utf8,
    }

    public class Configuration_Translation
    {
        [Tooltip("Solve merge conflicts between translated strings and other plugins.")]
        public bool Merge { get; set; } = true;

        public bool Verbose { get; set; } = true;

        [Tooltip("Forcefully revert string-related changes made in plugins.")]
        public bool ForceOverride { get; set; } = false;

        public Configuration_Records Records { get; set; } = new Configuration_Records();

        public List<ModKey> WhitelistPlugins { get; set; } = new();

        public List<ModKey> BlacklistPlugins { get; set; } = new();

        [Tooltip("Any plugin whose language differs should be either blacklisted or marked as translated.")]
        public Language OriginalLanguage { get; set; } = Language.English;

        /* TODO
        [Tooltip("Don't overwrite names / texts for mods that declare corresponding bash tags.")]
        public bool UseBashTags { get; set; } = false;

        [Tooltip("Paths to LOOT taglist (included with WB, used only if no LOOT masterlist could be found), LOOT masterlist, LOOT userlist.")]
        public List<string> BashTagLists { get; set; } = new();
        */

        [Tooltip("Language used for translation.")]
        public Language Language { get; set; } = Language.English;

        [Tooltip("Encoding used for translation.")]
        public Encoding Encoding { get; set; } = Encoding._1252;

        [Tooltip("Translated plugins (both STRINGS and ESP/L-patches).")]
        public List<ModKey> Plugins { get; set; } = new()
        {
            "Skyrim.esm",
            "Update.esm",
            "Dawnguard.esm",
            "HearthFires.esm",
            "Dragonborn.esm",
            "Unofficial Skyrim Special Edition Patch.esp",
            "ccBGSSSE001-Fish.esm",
            "ccQDRSSE001-SurvivalMode.esl",
            "ccBGSSSE037-Curios.esl",
            "ccBGSSSE025-AdvDSGS.esm"
        };

        [Tooltip("Copy (as override) all winning records from selected plugins (even if they are not changed).")]
        public List<ModKey> CopyPlugins { get; set; } = new()
        {
        };
    }

    public class Configuration_Records
    {
        [Tooltip("String parameters of scripts that are defined inside of various records.")]
        public bool Script { get; set; } = true;

        // places
        public bool Cell { get; set; } = true;
        public bool Worldspace { get; set; } = true;
        public bool Location { get; set; } = true;

        // actors
        public bool Npc { get; set; } = true;
        public bool ActorValueInformation { get; set; } = true;
        public bool BodyPartData { get; set; } = true;
        public bool Class { get; set; } = true;
        public bool Perk { get; set; } = true;
        public bool Race { get; set; } = true;

        // items
        public bool Armor { get; set; } = true;
        public bool Weapon { get; set; } = true;
        public bool Ammunition { get; set; } = true;
        public bool Projectile { get; set; } = true;
        public bool Book { get; set; } = true;
        public bool Scroll { get; set; } = true;
        public bool Ingredient { get; set; } = true;
        public bool Ingestible { get; set; } = true;

        // interactibles
        public bool AlchemicalApparatus { get; set; } = true;
        public bool Flora { get; set; } = true;
        public bool Activator { get; set; } = true;
        public bool TalkingActivator { get; set; } = true;
        public bool Door { get; set; } = true;

        // containers
        public bool Container { get; set; } = true;
        public bool Furniture { get; set; } = true;

        // misc
        public bool Key { get; set; } = true;
        public bool MiscItem { get; set; } = true;
        public bool SoulGem { get; set; } = true;
        public bool Light { get; set; } = true;

        // effects
        public bool ObjectEffect { get; set; } = true;
        public bool Spell { get; set; } = true;
        public bool MagicEffect { get; set; } = true;
        public bool Shout { get; set; } = true;
        public bool WordOfPower { get; set; } = true;
        public bool Hazard { get; set; } = true;
        public bool Explosion { get; set; } = true;

        // text
        public bool Dialog { get; set; } = true;
        public bool LoadScreen { get; set; } = true;
        public bool Message { get; set; } = true;
        public bool Quest { get; set; } = true;
        public bool GameSetting { get; set; } = true;
    }
}