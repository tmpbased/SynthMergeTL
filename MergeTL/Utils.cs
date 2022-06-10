using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using System.Collections.Generic;
using System.Linq;

namespace MergeTL
{
    public class Utils
    {
        public const Language OriginalLanguage = Language.English;

        public static string Key(IMajorRecordGetter majorRecordGetter, string name) => $"{majorRecordGetter.FormKey}→{name}";

        public static string Key(IDialogResponsesGetter dialogResponsesGetter, IDialogResponseGetter dialogResponseGetter) =>
            $"{dialogResponsesGetter.FormKey}→Response({dialogResponseGetter.ResponseNumber})→NAM1";

        public static string Key(IBodyPartDataGetter bodyPartDataGetter, IBodyPartGetter bodyPartGetter) =>
            $"{bodyPartDataGetter.FormKey}→BodyPart({bodyPartGetter.PartNode})→BPTN";

        public static string Key(IMessageGetter messageGetter, int i) => $"{messageGetter.FormKey}→Button({i})→ITXT";

        public static string Key(IQuestGetter questGetter, IQuestStageGetter questStageGetter, int i) =>
            $"{questGetter.FormKey}→Stage({questStageGetter.Index})→Log({i})→CNAM";

        public static string Key(
            IMajorRecordGetter majorRecordGetter,
            IScriptEntryGetter scriptEntryGetter,
            IScriptStringPropertyGetter scriptStringPropertyGetter) =>
            $"{majorRecordGetter.FormKey}→Script({scriptEntryGetter.Name})→Property({scriptStringPropertyGetter.Name})";

        public static IEnumerable<(IScriptEntryGetter, IScriptStringPropertyGetter)> GetScriptStringProperties(IAVirtualMachineAdapterGetter? virtualMachineAdapterGetter)
        {
            foreach (IScriptEntryGetter scriptEntryGetter in virtualMachineAdapterGetter?.Scripts ?? Enumerable.Empty<IScriptEntryGetter>())
            {
                foreach (IScriptPropertyGetter scriptPropertyGetter in scriptEntryGetter.Properties)
                {
                    if (scriptPropertyGetter is IScriptStringPropertyGetter scriptStringPropertyGetter)
                    {
                        yield return (scriptEntryGetter, scriptStringPropertyGetter);
                    }
                }
            }
        }

        public static IEnumerable<(IScriptEntry, IScriptStringProperty)> GetScriptStringProperties(IAVirtualMachineAdapter? virtualMachineAdapter)
        {
            foreach (IScriptEntry scriptEntry in virtualMachineAdapter?.Scripts ?? Enumerable.Empty<IScriptEntryGetter>())
            {
                foreach (IScriptProperty scriptProperty in scriptEntry.Properties)
                {
                    if (scriptProperty is IScriptStringProperty scriptStringProperty)
                    {
                        yield return (scriptEntry, scriptStringProperty);
                    }
                }
            }
        }
    }
}
