using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace CoreBuild.Help
{
    public class Help : Task
    {
        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string HelpProperties { get; set; } = "true";

        [Required]
        public string HelpTargets { get; set; } = "true";

        [Required]
        public string HelpInclude { get; set; } = "*";

        public string HelpExclude { get; set; } = "";

        public override bool Execute()
        {
            var collection = new ProjectCollection();
            var root = ProjectRootElement.Open(ProjectFile, collection);
            var docs = new ConcurrentDictionary<string, XDocument>();
            var eval = new Project(root, null, null, collection);

            var metaHelp = new StringBuilder();
            metaHelp.Append("Help: properties to customize what help reporting");

            var help = new StringBuilder();
            var standard = eval.Targets.ContainsKey("Configure") &&
                eval.Targets.ContainsKey("Build") &&
                eval.Targets.ContainsKey("Test") &&
                eval.Targets.ContainsKey("Run");

            if (standard)
            {
                help.AppendLine("Standard: YES √ (Configure, Build, Test and Run targets supported)").AppendLine();
            }
            else
            {
                help.AppendLine("Standard: NO x (Missing Configure, Build, Test or Run targets. Lean more at http://corebuild.io").AppendLine();
                Log.LogWarning(null, "CB01", null, null, 0, 0, 0, 0, "This project is NOT CoreBuild Standard compatible. Please provide Configure, Build, Test and Run targets. Lean more at http://corebuild.io");
            }

            help.Append("Properties:");
            foreach (var prop in eval.Properties
                .Where(x => x.Xml != null && !x.Name.StartsWith("_"))
                .Where(x => Operators.LikeString(x.Name, HelpInclude, CompareMethod.Text))
                .Where(x => !Operators.LikeString(x.Name, HelpExclude, CompareMethod.Text))
                .OrderBy(x => x.Name))
            {
                var isMeta = Path.GetFileName(prop.Xml.Location.File) == "CoreBuild.Help.targets";
                var builder = isMeta ? metaHelp : help;

                // Skip non-meta props that don't match the include or match the exclude patterns
                if (!isMeta &&
                    (!Operators.LikeString(prop.Name, HelpInclude, CompareMethod.Text) ||
                    Operators.LikeString(prop.Name, HelpExclude, CompareMethod.Text)))
                    continue;

                builder.AppendLine().Append($"\t- {prop.Name}");
                var doc = docs.GetOrAdd(prop.Xml.Location.File, file => XDocument.Load(file, LoadOptions.SetLineInfo));
                var element = (from x in doc.Root.Descendants()
                               where x.Name.LocalName == prop.Name
                               let info = (IXmlLineInfo)x
                               where info.HasLineInfo() &&
                                 info.LineNumber == prop.Xml.Location.Line &&
                                 // LinePosition starts at 1, 0 means no information
                                 info.LinePosition == (prop.Xml.Location.Column + 1)
                               select x)
                              .First();

                if (element.PreviousNode != null && element.PreviousNode.NodeType == XmlNodeType.Comment)
                {
                    var comment = (XComment)element.PreviousNode;
                    AppendComment(builder, prop.Name, comment.Value);
                }
            }

            help.AppendLine().AppendLine().Append("Targets:");
            foreach (var target in eval.Targets
                .Where(x => x.Key != "Help" && !x.Key.StartsWith("_")).OrderBy(x => x.Key)
                .Where(x => Operators.LikeString(x.Key, HelpInclude, CompareMethod.Text))
                .Where(x => !Operators.LikeString(x.Key, HelpExclude, CompareMethod.Text))
                .OrderBy(x => x.Key))
            {
                help.AppendLine().Append($"\t- {target.Key}");

                var doc = docs.GetOrAdd(target.Value.Location.File, file => XDocument.Load(file, LoadOptions.SetLineInfo));
                var element = (from x in doc.Root.Descendants()
                               where x.Name.LocalName == "Target"
                               let info = (IXmlLineInfo)x
                               where info.HasLineInfo() &&
                                 info.LineNumber == target.Value.Location.Line &&
                                 // LinePosition starts at 1, 0 means no information
                                 info.LinePosition == (target.Value.Location.Column + 1)
                               select x)
                              .First();

                if (element.PreviousNode != null && element.PreviousNode.NodeType == XmlNodeType.Comment)
                {
                    var comment = (XComment)element.PreviousNode;
                    AppendComment(help, target.Key, comment.Value);
                }
            }

            help.AppendLine();
            metaHelp.AppendLine();
            Log.LogMessage(MessageImportance.High, help.ToString() + Environment.NewLine + metaHelp.ToString());

            return true;
        }

        // Removes common wrapper characters in target and property comment blocks.
        static readonly Regex CommentBlockExpr = new Regex("[=|*]{3,}", RegexOptions.Compiled);

        void AppendComment(StringBuilder help, string name, string comment)
        {
            var indent = "\t" + new string(' ', name.Length + 4);
            help.Append(": ");
            var first = true;
            foreach (var line in comment.Trim()
                .Split(new[] { Environment.NewLine, '\n'.ToString() }, StringSplitOptions.None)
                .Where(x => !CommentBlockExpr.IsMatch(x)))
            {
                if (first)
                {
                    help.Append(line.Trim());
                    first = false;
                }
                else
                {
                    help.AppendLine().Append(indent).Append(line.Trim());
                }
            }
        }
    }
}
