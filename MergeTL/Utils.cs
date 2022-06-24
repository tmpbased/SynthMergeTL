using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Strings.DI;
using System.Collections.Generic;
using System.Linq;

namespace MergeTL
{
    public class Utils
    {
        public static IMutagenEncoding GetEncoding(Encoding encoding)
        {
            switch (encoding)
            {
                case Encoding._932:
                    return MutagenEncodingProvider._932;
                case Encoding._1250:
                    return MutagenEncodingProvider._1250;
                case Encoding._1251:
                    return MutagenEncodingProvider._1251;
                case Encoding._1252:
                    return MutagenEncodingProvider._1252;
                case Encoding._1253:
                    return MutagenEncodingProvider._1253;
                case Encoding._1254:
                    return MutagenEncodingProvider._1254;
                case Encoding._1256:
                    return MutagenEncodingProvider._1256;
                case Encoding._utf8:
                default:
                    return MutagenEncodingProvider._utf8;
            }
        }

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
