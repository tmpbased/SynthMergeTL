using System;
using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using System.Text.RegularExpressions;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Plugins.Records;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Noggog;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Archives.Exceptions;
using System.Threading;
using Mutagen.Bethesda.Strings.DI;
using System.Reflection;
using Mutagen.Bethesda.Environments;
using System.Linq;
using System.Text;

namespace MergeTL.Tasks
{
    public class MergeTranslation
    {
        private const Language OriginalLanguage = Utils.OriginalLanguage;
        private readonly Configuration settings;

        public MergeTranslation(Configuration settings)
        {
            this.settings = settings;
        }

        public void Run(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            ILoadOrder<IModListing<ISkyrimModGetter>> translatedLoadOrder = LoadOrder.Import<ISkyrimModGetter>(state.DataFolderPath, settings.Translation.Plugins, GameRelease.SkyrimSE);
            ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder = state.LoadOrder;
            if (settings.Translation.WhitelistPlugins.Count > 0)
            {
                loadOrder = LoadOrder.Import<ISkyrimModGetter>(state.DataFolderPath, settings.Translation.WhitelistPlugins, GameRelease.SkyrimSE);
            }
            else if (settings.Translation.BlacklistPlugins.Count > 0)
            {
                foreach (ModKey modKey in settings.Translation.BlacklistPlugins)
                {
                    loadOrder.RemoveKey(modKey);
                }
            }
            ILinkCache linkCache = loadOrder.ListedOrder.ToImmutableLinkCache();
            Console.Write("Plugins:");
            foreach (var key in loadOrder.Keys)
            {
                Console.Write($" {key}");
            }
            Console.WriteLine($" ({loadOrder.Count})");
            TranslationContext translationContext = new(state, settings, loadOrder, linkCache, translatedLoadOrder.ListedOrder.ToImmutableLinkCache());
            // places
            if (settings.Translation.Records.Cell)
            {
                foreach (var cellContext in loadOrder.PriorityOrder.Cell().WinningContextOverrides(linkCache))
                {
                    Merge(translationContext, cellContext,
                        CreateMajorRecordLens(cellContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            if (settings.Translation.Records.Worldspace)
            {
                foreach (var worldspaceContext in loadOrder.PriorityOrder.Worldspace().WinningContextOverrides())
                {
                    Merge(translationContext, worldspaceContext,
                        CreateMajorRecordLens(worldspaceContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            if (settings.Translation.Records.Location)
            {
                foreach (var locationContext in loadOrder.PriorityOrder.Location().WinningContextOverrides())
                {
                    Merge(translationContext, locationContext,
                        CreateMajorRecordLens(locationContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            // actors
            if (settings.Translation.Records.Npc)
            {
                foreach (var npcContext in loadOrder.PriorityOrder.Npc().WinningContextOverrides())
                {
                    Merge(translationContext, npcContext,
                        CreateMajorRecordLens(npcContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(npcContext, "SHRT", it => it.ShortName, (it, value) => it.ShortName = value),
                        CreateScriptStringPropertyDataLens(npcContext));
                }
            }
            if (settings.Translation.Records.ActorValueInformation)
            {
                foreach (var actorValueInformationContext in loadOrder.PriorityOrder.ActorValueInformation().WinningContextOverrides())
                {
                    Merge(translationContext, actorValueInformationContext,
                        CreateMajorRecordLens(actorValueInformationContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(actorValueInformationContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.BodyPartData)
            {
                foreach (var bodyPartDataContext in loadOrder.PriorityOrder.BodyPartData().WinningContextOverrides())
                {
                    Merge(translationContext, bodyPartDataContext, CreateBodyPartNameLens());
                }
            }
            if (settings.Translation.Records.Class)
            {
                foreach (var classContext in loadOrder.PriorityOrder.Class().WinningContextOverrides())
                {
                    Merge(translationContext, classContext,
                        CreateMajorRecordLens(classContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                    // TODO "DESC", classContext.Record.Description
                }
            }
            if (settings.Translation.Records.Perk)
            {
                foreach (var perkContext in loadOrder.PriorityOrder.Perk().WinningContextOverrides())
                {
                    Merge(translationContext, perkContext,
                        CreateMajorRecordLens(perkContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(perkContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.Race)
            {
                foreach (var raceContext in loadOrder.PriorityOrder.Race().WinningContextOverrides())
                {
                    Merge(translationContext, raceContext,
                        CreateMajorRecordLens(raceContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(raceContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            // items
            if (settings.Translation.Records.Armor)
            {
                foreach (var armorContext in loadOrder.PriorityOrder.Armor().WinningContextOverrides())
                {
                    Merge(translationContext, armorContext,
                        CreateMajorRecordLens(armorContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(armorContext, "DESC", it => it.Description, (it, value) => it.Description = value),
                        CreateScriptStringPropertyDataLens(armorContext));
                }
            }
            if (settings.Translation.Records.Weapon)
            {
                foreach (var weaponContext in loadOrder.PriorityOrder.Weapon().WinningContextOverrides())
                {
                    Merge(translationContext, weaponContext,
                        CreateMajorRecordLens(weaponContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(weaponContext, "DESC", it => it.Description, (it, value) => it.Description = value),
                        CreateScriptStringPropertyDataLens(weaponContext));
                }
            }
            if (settings.Translation.Records.Ammunition)
            {
                foreach (var ammunitionContext in loadOrder.PriorityOrder.Ammunition().WinningContextOverrides())
                {
                    Merge(translationContext, ammunitionContext,
                        CreateMajorRecordLens(ammunitionContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(ammunitionContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.Projectile)
            {
                foreach (var projectileContext in loadOrder.PriorityOrder.Projectile().WinningContextOverrides())
                {
                    Merge(translationContext, projectileContext,
                        CreateMajorRecordLens(projectileContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            if (settings.Translation.Records.Book)
            {
                foreach (var bookContext in loadOrder.PriorityOrder.Book().WinningContextOverrides())
                {
                    Merge(translationContext, bookContext,
                        CreateMajorRecordLens(bookContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(bookContext, "DESC", it => it.BookText, (it, value) => it.BookText = value),
                        CreateMajorRecordLens(bookContext, "CNAM", it => it.Description, (it, value) => it.Description = value),
                        CreateScriptStringPropertyDataLens(bookContext));
                }
            }
            if (settings.Translation.Records.Scroll)
            {
                foreach (var scrollContext in loadOrder.PriorityOrder.Scroll().WinningContextOverrides())
                {
                    Merge(translationContext, scrollContext,
                        CreateMajorRecordLens(scrollContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(scrollContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.Ingredient)
            {
                foreach (var ingredientContext in loadOrder.PriorityOrder.Ingredient().WinningContextOverrides())
                {
                    Merge(translationContext, ingredientContext,
                        CreateMajorRecordLens(ingredientContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(ingredientContext));
                }
            }
            if (settings.Translation.Records.Ingestible)
            {
                foreach (var ingestibleContext in loadOrder.PriorityOrder.Ingestible().WinningContextOverrides())
                {
                    Merge(translationContext, ingestibleContext,
                        CreateMajorRecordLens(ingestibleContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(ingestibleContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            // interactibles
            if (settings.Translation.Records.AlchemicalApparatus)
            {
                foreach (var alchemicalApparatusContext in loadOrder.PriorityOrder.AlchemicalApparatus().WinningContextOverrides())
                {
                    Merge(translationContext, alchemicalApparatusContext,
                        CreateMajorRecordLens(alchemicalApparatusContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(alchemicalApparatusContext, "DESC", it => it.Description, (it, value) => it.Description = value),
                        CreateScriptStringPropertyDataLens(alchemicalApparatusContext));
                }
            }
            if (settings.Translation.Records.Flora)
            {
                foreach (var floraContext in loadOrder.PriorityOrder.Flora().WinningContextOverrides())
                {
                    Merge(translationContext, floraContext,
                        CreateMajorRecordLens(floraContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(floraContext, "RNAM", it => it.ActivateTextOverride, (it, value) => it.ActivateTextOverride = value),
                        CreateScriptStringPropertyDataLens(floraContext));
                }
            }
            if (settings.Translation.Records.Activator)
            {
                foreach (var activatorContext in loadOrder.PriorityOrder.Activator().WinningContextOverrides())
                {
                    Merge(translationContext, activatorContext,
                        CreateMajorRecordLens(activatorContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(activatorContext, "RNAM", it => it.ActivateTextOverride, (it, value) => it.ActivateTextOverride = value),
                        CreateScriptStringPropertyDataLens(activatorContext));
                }
            }
            if (settings.Translation.Records.TalkingActivator)
            {
                foreach (var talkingActivatorContext in loadOrder.PriorityOrder.TalkingActivator().WinningContextOverrides())
                {
                    Merge(translationContext, talkingActivatorContext,
                        CreateMajorRecordLens(talkingActivatorContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(talkingActivatorContext));
                }
            }
            if (settings.Translation.Records.Door)
            {
                foreach (var doorContext in loadOrder.PriorityOrder.Door().WinningContextOverrides())
                {
                    Merge(translationContext, doorContext,
                        CreateMajorRecordLens(doorContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(doorContext));
                }
            }
            // containers
            if (settings.Translation.Records.Container)
            {
                foreach (var containerContext in loadOrder.PriorityOrder.Container().WinningContextOverrides())
                {
                    Merge(translationContext, containerContext,
                        CreateMajorRecordLens(containerContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(containerContext));
                }
            }
            if (settings.Translation.Records.Furniture)
            {
                foreach (var furnitureContext in loadOrder.PriorityOrder.Furniture().WinningContextOverrides())
                {
                    Merge(translationContext, furnitureContext,
                        CreateMajorRecordLens(furnitureContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(furnitureContext));
                }
            }
            // misc
            if (settings.Translation.Records.Key)
            {
                foreach (var keyContext in loadOrder.PriorityOrder.Key().WinningContextOverrides())
                {
                    Merge(translationContext, keyContext,
                        CreateMajorRecordLens(keyContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(keyContext));
                }
            }
            if (settings.Translation.Records.MiscItem)
            {
                foreach (var miscItemContext in loadOrder.PriorityOrder.MiscItem().WinningContextOverrides())
                {
                    Merge(translationContext, miscItemContext,
                        CreateMajorRecordLens(miscItemContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateScriptStringPropertyDataLens(miscItemContext));
                }
            }
            if (settings.Translation.Records.SoulGem)
            {
                foreach (var soulGemContext in loadOrder.PriorityOrder.SoulGem().WinningContextOverrides())
                {
                    Merge(translationContext, soulGemContext,
                        CreateMajorRecordLens(soulGemContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            // effects
            if (settings.Translation.Records.ObjectEffect)
            {
                foreach (var objectEffectContext in loadOrder.PriorityOrder.ObjectEffect().WinningContextOverrides())
                {
                    Merge(translationContext, objectEffectContext,
                        CreateMajorRecordLens(objectEffectContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            if (settings.Translation.Records.Spell)
            {
                foreach (var spellContext in loadOrder.PriorityOrder.Spell().WinningContextOverrides())
                {
                    Merge(translationContext, spellContext,
                        CreateMajorRecordLens(spellContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(spellContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.MagicEffect)
            {
                foreach (var magicEffectContext in loadOrder.PriorityOrder.MagicEffect().WinningContextOverrides())
                {
                    Merge(translationContext, magicEffectContext,
                        CreateMajorRecordLens(magicEffectContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(magicEffectContext, "DNAM", it => it.Description, (it, value) => it.Description = value),
                        CreateScriptStringPropertyDataLens(magicEffectContext));
                }
            }
            if (settings.Translation.Records.Shout)
            {
                foreach (var shoutContext in loadOrder.PriorityOrder.Shout().WinningContextOverrides())
                {
                    Merge(translationContext, shoutContext,
                        CreateMajorRecordLens(shoutContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(shoutContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.WordOfPower)
            {
                foreach (var wordOfPowerContext in loadOrder.PriorityOrder.WordOfPower().WinningContextOverrides())
                {
                    Merge(translationContext, wordOfPowerContext,
                        CreateMajorRecordLens(wordOfPowerContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(wordOfPowerContext, "TNAM", it => it.Translation, (it, value) => it.Translation = value));
                }
            }
            if (settings.Translation.Records.Hazard)
            {
                foreach (var hazardContext in loadOrder.PriorityOrder.Hazard().WinningContextOverrides())
                {
                    Merge(translationContext, hazardContext,
                        CreateMajorRecordLens(hazardContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
            }
            // text
            if (settings.Translation.Records.Dialog)
            {
                foreach (var dialogTopicContext in loadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
                {
                    Merge(translationContext, dialogTopicContext,
                        CreateMajorRecordLens(dialogTopicContext, "FULL", it => it.Name, (it, value) => it.Name = value));
                }
                foreach (var dialogResponsesContext in loadOrder.PriorityOrder.DialogResponses().WinningContextOverrides(linkCache))
                {
                    Merge(translationContext, dialogResponsesContext,
                        CreateMajorRecordLens(dialogResponsesContext, "RNAM", it => it.Prompt, (it, value) => it.Prompt = value),
                        CreateDialogResponseTextLens(),
                        CreateScriptStringPropertyDataLens(dialogResponsesContext, it => it.VirtualMachineAdapter));
                }
            }
            if (settings.Translation.Records.LoadScreen)
            {
                foreach (var loadScreenContext in loadOrder.PriorityOrder.LoadScreen().WinningContextOverrides())
                {
                    Merge(translationContext, loadScreenContext,
                        CreateMajorRecordLens(loadScreenContext, "DESC", it => it.Description, (it, value) => it.Description = value));
                }
            }
            if (settings.Translation.Records.Message)
            {
                foreach (var messageContext in loadOrder.PriorityOrder.Message().WinningContextOverrides())
                {
                    Merge(translationContext, messageContext,
                        CreateMajorRecordLens(messageContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(messageContext, "DESC", it => it.Description, (it, value) => it.Description = value),
                        CreateMessageButtonTextLens());
                }
            }
            if (settings.Translation.Records.Quest)
            {
                foreach (var questContext in loadOrder.PriorityOrder.Quest().WinningContextOverrides())
                {
                    Merge(translationContext, questContext,
                        CreateMajorRecordLens(questContext, "FULL", it => it.Name, (it, value) => it.Name = value),
                        CreateMajorRecordLens(questContext, "DESC", it => it.Description, (it, value) => it.Description = value),
                        CreateQuestStageLogEntryLens(),
                        CreateScriptStringPropertyDataLens(questContext, it => it.VirtualMachineAdapter));
                }
            }
        }

        private delegate ITranslatedStringGetter? ValueGetter<T>(T majorRecordGetter) where T : IMajorRecordGetter;
        private delegate void ValueSetter<T>(T majorRecord, TranslatedString value) where T : IMajorRecord;

        private class TranslationContext
        {
            private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> state;
            private readonly Configuration settings;
            private readonly ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder;
            private readonly ILinkCache linkCache;
            private readonly ILinkCache translatedLinkCache;
            private readonly Dictionary<ModKey, IModStringsLookup> stringsLookups = new();
            private readonly Dictionary<ModKey, ILinkCache> modLinkCaches = new();

            public TranslationContext(
                IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
                Configuration settings,
                ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder,
                ILinkCache linkCache,
                ILinkCache translatedLinkCache)
            {
                this.state = state;
                this.settings = settings;
                this.loadOrder = loadOrder;
                this.linkCache = linkCache;
                this.translatedLinkCache = translatedLinkCache;
            }

            public W GetOrAddAsOverride<R, W>(IModContext<ISkyrimMod, ISkyrimModGetter, W, R> modContext)
                where R : class, IMajorRecordGetter
                where W : class, R, IMajorRecord
            {
                return modContext.GetOrAddAsOverride(state.PatchMod);
            }

            public delegate bool TryLookup<R, W>(Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str)
                where R : class, IMajorRecordGetter
                where W : class, R, IMajorRecord;

            public (IModContext<R>?, TryLookup<R, W>) GetOriginalContext<R, W>(IModContext<ISkyrimMod, ISkyrimModGetter, W, R> context)
                where R : class, IMajorRecordGetter
                where W : class, R, IMajorRecord
            {
                linkCache.TryResolveSimpleContext(context.Record.FormKey, out IModContext<R>? originalContext, ResolveTarget.Origin);
                if (originalContext == null)
                {
                    return (null, (Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str) =>
                    {
                        str = null;
                        return false;
                    }
                    );
                }
                if (settings.Translation.Plugins.Contains(originalContext.ModKey))
                {
                    translatedLinkCache.TryResolveSimpleContext(context.Record.FormKey, out originalContext, ResolveTarget.Winner);
                    var loopContext = originalContext;
                    while (loopContext is not null && settings.Translation.Plugins.Contains(loopContext.ModKey) && GetModStringsLookup(loopContext.ModKey).Empty)
                    {
                        if (!modLinkCaches.TryGetValue(loopContext.ModKey, out ILinkCache? modLinkCache))
                        {
                            if (loadOrder.TryGetIfEnabledAndExists(loopContext.ModKey, out var mod))
                            {
                                var masters = new List<ModKey>();
                                foreach (var masterMod in mod.MasterReferences)
                                {
                                    masters.Add(masterMod.Master);
                                }
                                var modLoadOrder = LoadOrder.Import<ISkyrimModGetter>(state.DataFolderPath, masters, GameRelease.SkyrimSE);
                                modLinkCache = modLoadOrder.ListedOrder.ToImmutableLinkCache();
                                modLinkCaches.Add(loopContext.ModKey, modLinkCache);
                            }
                        }
                        if (modLinkCache is null)
                        {
                            loopContext = null;
                        }
                        else
                        {
                            modLinkCache.TryResolveSimpleContext(context.Record.FormKey, out loopContext, ResolveTarget.Winner);
                        }
                    }
                    if (loopContext is null)
                    {
                        return (null, (Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str) =>
                        {
                            str = null;
                            return false;
                        }
                        );
                    }
                    else if (GetModStringsLookup(loopContext.ModKey).Empty)
                    {
                        return (loopContext, (Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str) =>
                            value.TryLookup(language, out str));
                    }
                    else
                    {
                        return (loopContext, (Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str) =>
                        {
                            if (value.TryLookup(GetModStringsLookup(loopContext.ModKey), language, out str))
                            {
                                return true;
                            }
                            str = null;
                            return false;
                        }
                        );
                    }
                }
                return (originalContext, (Lens<R, W>.Value value, Language language, [MaybeNullWhen(false)] out string str) => value.TryLookup(language, out str));
            }

            public IModContext<R>? GetTranslatedContext<R>(R majorRecordGetter) where R : class, IMajorRecordGetter =>
                translatedLinkCache.TryResolveSimpleContext(majorRecordGetter.FormKey, out IModContext<R>? getter) ? getter : null;

            private IModStringsLookup GetModStringsLookup(ModKey modKey)
            {
                if (stringsLookups.TryGetValue(modKey, out IModStringsLookup? stringsLookup))
                {
                    return stringsLookup;
                }
                stringsLookup = BSAStringsLookup.Open(state.GameRelease, modKey, state.DataFolderPath);
                stringsLookups.Add(modKey, stringsLookup);
                return stringsLookup;
            }
        }

        private delegate IAVirtualMachineAdapterGetter? VirtualMachineAdapterGetter<T>(T majorRecordGetter) where T : IMajorRecordGetter;

        private abstract class Lens<R, W>
            where R : class, IMajorRecordGetter
            where W : class, R, IMajorRecord
        {
            protected readonly MergeTranslation parent;

            protected Lens(MergeTranslation parent)
            {
                this.parent = parent;
            }

            public delegate void ValueSetter(string value);

            public abstract class Value
            {
                public abstract bool TryLookup(Language language, [MaybeNullWhen(false)] out string str);

                public abstract bool TryLookup(IModStringsLookup lookup, Language language, [MaybeNullWhen(false)] out string str);

                public static implicit operator Value(string str) => new Static(str);

                class Static : Value
                {
                    private readonly string? str;

                    public Static(string? str)
                    {
                        this.str = str;
                    }

                    public override bool TryLookup(Language language, [MaybeNullWhen(false)] out string str)
                    {
                        str = this.str;
                        return str != null;
                    }

                    public override bool TryLookup(IModStringsLookup lookup, Language language, [MaybeNullWhen(false)] out string str)
                    {
                        str = this.str;
                        return str != null;
                    }
                }

                public class Translated : Value
                {
                    private readonly ITranslatedStringGetter str;

                    public Translated(ITranslatedStringGetter str)
                    {
                        this.str = str;
                    }

                    // TODO probably shouldn't be hardcoded...
                    private static string Utf8To1252(string str)
                    {
                        var arraySpan = new Span<byte>(new byte[MutagenEncodingProvider._utf8.GetByteCount(str.AsSpan())]);
                        MutagenEncodingProvider._utf8.GetBytes(str.AsSpan(), arraySpan);
                        return MutagenEncodingProvider._1252.GetString(arraySpan);
                    }

                    public override bool TryLookup(Language language, [MaybeNullWhen(false)] out string str)
                    {
                        if (this.str.TryLookup(language, out str))
                        {
                            // if UsingLocalizationDictionary (a.k.a. STRINGS), de-encode to 1252 :-)
                            if ((this.str as TranslatedString)?.StringsKey is not null)
                            {
                                str = Utf8To1252(str);
                            }
                            return true;
                        }
                        return false;
                    }

                    public override bool TryLookup(IModStringsLookup lookup, Language language, [MaybeNullWhen(false)] out string str)
                    {
                        uint? stringsKey = (this.str as TranslatedString)?.StringsKey;
                        if (stringsKey is null)
                        {
                            str = null;
                            return false;
                        }
                        StringsSource? stringsSource = this.str.GetType()
                            .GetField("StringsSource", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?
                            .GetValue(this.str) as StringsSource?;
                        if (stringsSource is null)
                        {
                            str = null;
                            return false;
                        }
                        if (lookup.TryLookup((StringsSource)stringsSource, language, (uint)stringsKey, out str))
                        {
                            // str = Utf8To1252(str);
                            return true;
                        }
                        return false;
                    }
                }
            }

            public abstract IEnumerable<(string, Value)> ReadKeyValue(R majorRecordGetter);

            public abstract IEnumerable<(string, ValueSetter)> WriteKeyValue(W majorRecord);
        }

        private MajorRecordLens<R, W> CreateMajorRecordLens<R, W>(IModContext<ISkyrimMod, ISkyrimModGetter, W, R> _, string name, ValueGetter<R> valueGetter, ValueSetter<W> valueSetter)
            where R : class, IMajorRecordGetter
            where W : class, R, IMajorRecord
        {
            return new MajorRecordLens<R, W>(this, name, valueGetter, valueSetter);
        }

        private class MajorRecordLens<R, W> : Lens<R, W>
            where R : class, IMajorRecordGetter
            where W : class, R, IMajorRecord
        {
            private readonly string name;
            private readonly ValueGetter<R> valueGetter;
            private readonly ValueSetter<W> valueSetter;

            public MajorRecordLens(MergeTranslation parent, string name, ValueGetter<R> valueGetter, ValueSetter<W> valueSetter) : base(parent)
            {
                this.name = name;
                this.valueGetter = valueGetter;
                this.valueSetter = valueSetter;
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(R majorRecordGetter)
            {
                ITranslatedStringGetter? value = valueGetter(majorRecordGetter);
                if (value != null)
                {
                    yield return (Utils.Key(majorRecordGetter, name), new Value.Translated(value));
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(W majorRecord)
            {
                yield return (Utils.Key(majorRecord, name), it =>
                {
                    if (valueGetter(majorRecord) is not TranslatedString value)
                    {
                        value = new TranslatedString(TranslatedString.DefaultLanguage);
                        valueSetter(majorRecord, value);
                    }
                    value.Set(parent.settings.Translation.Language, it);
                }
                );
            }
        }

        private ScriptStringPropertyDataLens<R, W> CreateScriptStringPropertyDataLens<R, W>(
            IModContext<ISkyrimMod, ISkyrimModGetter, W, R> _)
            where R : class, IMajorRecordGetter, IScriptedGetter
            where W : class, R, IMajorRecord
        {
            return new ScriptStringPropertyDataLens<R, W>(this, it => it.VirtualMachineAdapter);
        }

        private ScriptStringPropertyDataLens<R, W> CreateScriptStringPropertyDataLens<R, W>(
            IModContext<ISkyrimMod, ISkyrimModGetter, W, R> _,
            VirtualMachineAdapterGetter<R> virtualMachineAdapterGetter)
            where R : class, IMajorRecordGetter
            where W : class, R, IMajorRecord
        {
            return new ScriptStringPropertyDataLens<R, W>(this, virtualMachineAdapterGetter);
        }

        private class ScriptStringPropertyDataLens<R, W> : Lens<R, W>
            where R : class, IMajorRecordGetter
            where W : class, R, IMajorRecord
        {
            private readonly VirtualMachineAdapterGetter<R> virtualMachineAdapterGetter;

            public ScriptStringPropertyDataLens(MergeTranslation parent, VirtualMachineAdapterGetter<R> virtualMachineAdapterGetter) : base(parent)
            {
                this.virtualMachineAdapterGetter = virtualMachineAdapterGetter;
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(R majorRecordGetter)
            {
                if (parent.settings.Translation.Records.Script)
                {
                    foreach (var (scriptEntryGetter, scriptStringPropertyGetter) in Utils.GetScriptStringProperties(virtualMachineAdapterGetter(majorRecordGetter)))
                    {
                        string key = Utils.Key(majorRecordGetter, scriptEntryGetter, scriptStringPropertyGetter);
                        yield return (key, scriptStringPropertyGetter.Data);
                    }
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(W majorRecord)
            {
                if (parent.settings.Translation.Records.Script)
                {
                    foreach (var (scriptEntry, scriptStringProperty) in Utils.GetScriptStringProperties(virtualMachineAdapterGetter(majorRecord) as IAVirtualMachineAdapter))
                    {
                        string key = Utils.Key(majorRecord, scriptEntry, scriptStringProperty);
                        yield return (key, value => scriptStringProperty.Data = value);
                    }
                }
            }
        }

        private BodyPartNameLens CreateBodyPartNameLens()
        {
            return new BodyPartNameLens(this);
        }

        private class BodyPartNameLens : Lens<IBodyPartDataGetter, IBodyPartData>
        {
            public BodyPartNameLens(MergeTranslation parent) : base(parent)
            {
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(IBodyPartDataGetter majorRecordGetter)
            {
                foreach (IBodyPartGetter bodyPartGetter in majorRecordGetter.Parts)
                {
                    string key = Utils.Key(majorRecordGetter, bodyPartGetter);
                    yield return (key, new Value.Translated(bodyPartGetter.Name));
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(IBodyPartData majorRecord)
            {
                foreach (IBodyPart bodyPartGetter in majorRecord.Parts)
                {
                    string key = Utils.Key(majorRecord, bodyPartGetter);
                    yield return (key, value => bodyPartGetter.Name.Set(parent.settings.Translation.Language, value));
                }
            }
        }

        private MessageButtonTextLens CreateMessageButtonTextLens()
        {
            return new MessageButtonTextLens(this);
        }

        // Let's just hope that nobody inserts new buttons in the middle...
        // Apparently, there is no reliable way to identify buttons' reorderings.
        private class MessageButtonTextLens : Lens<IMessageGetter, IMessage>
        {
            public MessageButtonTextLens(MergeTranslation parent) : base(parent)
            {
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(IMessageGetter majorRecordGetter)
            {
                int i = 0;
                foreach (IMessageButtonGetter messageButtonGetter in majorRecordGetter.MenuButtons)
                {
                    string key = Utils.Key(majorRecordGetter, i++);
                    if (messageButtonGetter.Text is not null)
                    {
                        yield return (key, new Value.Translated(messageButtonGetter.Text));
                    }
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(IMessage majorRecord)
            {
                int i = 0;
                foreach (IMessageButton messageButton in majorRecord.MenuButtons)
                {
                    string key = Utils.Key(majorRecord, i++);
                    yield return (key, it =>
                    {
                        if (messageButton.Text is not TranslatedString value)
                        {
                            value = new TranslatedString(TranslatedString.DefaultLanguage);
                            messageButton.Text = value;
                        }
                        value.Set(parent.settings.Translation.Language, it);
                    }
                    );
                }
            }
        }

        private QuestStageLogEntryLens CreateQuestStageLogEntryLens()
        {
            return new QuestStageLogEntryLens(this);
        }

        // Let's just hope that nobody inserts new quest log entries in the middle...
        // Apparently, there is no reliable way to identify entries' reorderings.
        private class QuestStageLogEntryLens : Lens<IQuestGetter, IQuest>
        {
            public QuestStageLogEntryLens(MergeTranslation parent) : base(parent)
            {
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(IQuestGetter majorRecordGetter)
            {
                foreach (IQuestStageGetter questStageGetter in majorRecordGetter.Stages)
                {
                    int i = 0;
                    foreach (IQuestLogEntryGetter questLogEntryGetter in questStageGetter.LogEntries)
                    {
                        string key = Utils.Key(majorRecordGetter, questStageGetter, i++);
                        if (questLogEntryGetter.Entry is not null)
                        {
                            yield return (key, new Value.Translated(questLogEntryGetter.Entry));
                        }
                    }
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(IQuest majorRecord)
            {
                foreach (IQuestStage questStage in majorRecord.Stages)
                {
                    int i = 0;
                    foreach (IQuestLogEntry questLogEntry in questStage.LogEntries)
                    {
                        string key = Utils.Key(majorRecord, questStage, i++);
                        yield return (key, it =>
                        {
                            if (questLogEntry.Entry is not TranslatedString value)
                            {
                                value = new TranslatedString(TranslatedString.DefaultLanguage);
                                questLogEntry.Entry = value;
                            }
                            value.Set(parent.settings.Translation.Language, it);
                        }
                        );
                    }
                }
            }
        }

        private DialogResponseTextLens CreateDialogResponseTextLens()
        {
            return new DialogResponseTextLens(this);
        }

        private class DialogResponseTextLens : Lens<IDialogResponsesGetter, IDialogResponses>
        {
            public DialogResponseTextLens(MergeTranslation parent) : base(parent)
            {
            }

            public override IEnumerable<(string, Value)> ReadKeyValue(IDialogResponsesGetter majorRecordGetter)
            {
                foreach (IDialogResponseGetter dialogResponseGetter in majorRecordGetter.Responses)
                {
                    string key = Utils.Key(majorRecordGetter, dialogResponseGetter);
                    yield return (key, new Value.Translated(dialogResponseGetter.Text));
                }
            }

            public override IEnumerable<(string, ValueSetter)> WriteKeyValue(IDialogResponses majorRecord)
            {
                foreach (IDialogResponse dialogResponse in majorRecord.Responses)
                {
                    string key = Utils.Key(majorRecord, dialogResponse);
                    yield return (key, value => dialogResponse.Text.Set(parent.settings.Translation.Language, value));
                }
            }
        }

        private void Merge<R, W>(
                TranslationContext translationContext,
                IModContext<ISkyrimMod, ISkyrimModGetter, W, R> modContext,
                params Lens<R, W>[] lenses)
                where R : class, IMajorRecordGetter
                where W : class, R, IMajorRecord
        {
            var translatedContext = translationContext.GetTranslatedContext(modContext.Record);
            if (translatedContext is null) return;
            Dictionary<string, string> translatedValues = new();
            foreach (var lens in lenses)
            {
                foreach (var (key, value) in lens.ReadKeyValue(translatedContext.Record))
                {
                    if (value != null && value.TryLookup(Language.English, out string? translatedValue) && Regex.IsMatch(translatedValue, @"\w"))
                    {
                        translatedValues.Add(key, translatedValue);
                    }
                }
            }
            if (translatedValues.Count <= 0) return;
            Lazy<Dictionary<string, string>> originalValues = settings.Translation.ForceOverride
                ? null!
                : new(() => GetOriginalValues<R, W>(translationContext, modContext, lenses));
            HashSet<string> replacementValues = new();
            foreach (var lens in lenses)
            {
                foreach (var (key, value) in lens.ReadKeyValue(modContext.Record))
                {
                    if (!translatedValues.TryGetValue(key, out string? translatedValue))
                    {
                        continue;
                    }
                    value.TryLookup(settings.Translation.Language, out string? modValue);
                    if (!string.Equals(translatedValue, modValue, StringComparison.Ordinal))
                    {
                        if (settings.Translation.ForceOverride)
                        {
                            Console.WriteLine(@$"{modContext.Record.Type.Name.Substring(1)} {modContext.Record.EditorID}({modContext.Record.FormKey})
    : {modValue}({modContext.ModKey})->{translatedValue}({translatedContext.ModKey})");
                            replacementValues.Add(key);
                        }
                        else
                        {
                            value.TryLookup(OriginalLanguage, out string? modOriginalValue);
                            originalValues.Value.TryGetValue(key, out string? originalValue);
                            if (string.Equals(CleanUpOriginalValue(originalValue), CleanUpOriginalValue(modOriginalValue), StringComparison.InvariantCultureIgnoreCase))
                            {
                                Console.WriteLine(@$"{modContext.Record.Type.Name.Substring(1)} {modContext.Record.EditorID}({modContext.Record.FormKey})
    : {originalValue}->{modOriginalValue}({modContext.ModKey})
    =>{modValue}({modContext.ModKey})->{translatedValue}({translatedContext.ModKey})");
                                replacementValues.Add(key);
                            }
                            else
                            {
                                Console.WriteLine(@$"{modContext.Record.Type.Name.Substring(1)} {modContext.Record.EditorID}({modContext.Record.FormKey})
    : {originalValue}->{modOriginalValue}({modContext.ModKey})
    =>{modValue}({modContext.ModKey})");
                            }
                        }
                    }
                }
            }
            if (replacementValues.Count <= 0) return;
            W majorRecord = translationContext.GetOrAddAsOverride(modContext);
            foreach (var lens in lenses)
            {
                foreach (var (key, valueSetter) in lens.WriteKeyValue(majorRecord))
                {
                    if (replacementValues.Contains(key) && translatedValues.TryGetValue(key, out string? translatedValue))
                    {
                        valueSetter(translatedValue);
                    }
                }
            }
        }

        [return: NotNullIfNotNull("value")]
        private static string? CleanUpOriginalValue(string? value)
        {
            if (value is null) return value;
            value = Regex.Replace(value, @"[^\w]an?([^\w]|$)", "$1");
            value = Regex.Replace(value, @"[^\w]the([^\w]|$)", "$1");
            value = Regex.Replace(value, @"[-.:,;!?\s]+", " ");
            return value.ToLower();
        }

        private static Dictionary<string, string> GetOriginalValues<R, W>(
            TranslationContext translationContext,
            IModContext<ISkyrimMod, ISkyrimModGetter, W, R> modContext,
            Lens<R, W>[] lenses)
                where R : class, IMajorRecordGetter
                where W : class, IMajorRecord, R
        {
            Dictionary<string, string> values = new();
            var (originalContext, originalValueLookup) = translationContext.GetOriginalContext(modContext);
            if (originalContext is not null)
            {
                foreach (var lens in lenses)
                {
                    foreach (var (key, value) in lens.ReadKeyValue(originalContext.Record))
                    {
                        if (originalValueLookup(value, OriginalLanguage, out string? originalValue))
                        {
                            values.Add(key, originalValue);
                        }
                    }
                }
            }
            return values;
        }
    }

    internal interface IModStringsLookup
    {
        bool Empty { get; }

        bool TryLookup(StringsSource source, Language language, uint key, [MaybeNullWhen(false)] out string str);
    }

    /** BSA only */
    internal class BSAStringsLookup : IModStringsLookup
    {
        private class DictionaryBundle
        {
            private readonly Dictionary<Language, Lazy<IStringsLookup>> _strings = new Dictionary<Language, Lazy<IStringsLookup>>();
            private readonly Dictionary<Language, Lazy<IStringsLookup>> _dlStrings = new Dictionary<Language, Lazy<IStringsLookup>>();
            private readonly Dictionary<Language, Lazy<IStringsLookup>> _ilStrings = new Dictionary<Language, Lazy<IStringsLookup>>();

            public bool Empty
            {
                get
                {
                    if (_strings.Count == 0 && _dlStrings.Count == 0)
                    {
                        return _ilStrings.Count == 0;
                    }

                    return false;
                }
            }

            public Dictionary<Language, Lazy<IStringsLookup>> Get(StringsSource source)
            {
                return source switch
                {
                    StringsSource.Normal => _strings,
                    StringsSource.IL => _ilStrings,
                    StringsSource.DL => _dlStrings,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        private readonly Lazy<DictionaryBundle> _dictionaries;

        public DirectoryPath DataPath { get; }

        public ModKey ModKey { get; }

        public bool Empty => _dictionaries.Value.Empty;

        private BSAStringsLookup(Lazy<DictionaryBundle> instantiator, DirectoryPath dataPath, ModKey modKey)
        {
            _dictionaries = instantiator;
            DataPath = dataPath;
            ModKey = modKey;
        }

        public static BSAStringsLookup Open(GameRelease release, ModKey modKey, DirectoryPath dataPath)
        {
            IMutagenEncodingProvider encodings = new MutagenEncodingProvider();
            return new BSAStringsLookup(new Lazy<DictionaryBundle>(delegate
            {
                DictionaryBundle dictionaryBundle = new DictionaryBundle();
                foreach (FilePath applicableArchivePath in Archive.GetApplicableArchivePaths(release, dataPath, modKey))
                {
                    try
                    {
                        if (Archive.CreateReader(release, applicableArchivePath).TryGetFolder("strings", out var folder))
                        {
                            try
                            {
                                foreach (IArchiveFile item in folder.Files)
                                {
                                    if (StringsUtility.TryRetrieveInfoFromString(release.GetLanguageFormat(), Path.GetFileName(item.Path), out var type, out var lang, out var modName2) &&
                                        MemoryExtensions.Equals(modKey.Name, modName2, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Dictionary<Language, Lazy<IStringsLookup>> dictionary = dictionaryBundle.Get(type);
                                        if (!dictionary.ContainsKey(lang))
                                        {
                                            dictionary[lang] = new Lazy<IStringsLookup>(delegate
                                            {
                                                try
                                                {
                                                    return new StringsLookupOverlay(item.GetMemorySlice(), type, encodings.GetEncoding(release, lang));
                                                }
                                                catch (Exception ex3)
                                                {
                                                    throw ArchiveException.EnrichWithFileAccessed("String file from BSA failed to parse", ex3, item.Path);
                                                }
                                            }, LazyThreadSafetyMode.ExecutionAndPublication);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) when (folder.Path != null)
                            {
                                throw ArchiveException.EnrichWithFolderAccessed("BSA folder failed to parse for string file", ex, folder.Path);
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        throw ArchiveException.EnrichWithArchivePath("BSA failed to parse for string file", ex2, applicableArchivePath);
                    }
                }

                return dictionaryBundle;
            }, isThreadSafe: true), dataPath, modKey);
        }

        public bool TryLookup(StringsSource source, Language language, uint key, [MaybeNullWhen(false)] out string str)
        {
            if (!Get(source).TryGetValue(language, out var value))
            {
                str = null;
                return false;
            }

            return value.Value.TryLookup(key, out str);
        }

        private Dictionary<Language, Lazy<IStringsLookup>> Get(StringsSource source)
        {
            return _dictionaries.Value.Get(source);
        }
    }
}
