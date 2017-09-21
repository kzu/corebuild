using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

namespace CoreBuild.Help
{
    public class Help : Task
    {
        [Required]
        public string HelpProject { get; set; }

        [Required]
        public string HelpImports { get; set; } = "false";

        [Required]
        public string HelpProperties { get; set; } = "true";

        [Required]
        public string HelpTargets { get; set; } = "true";

        [Required]
        public string HelpInclude { get; set; } = ".*";

        [Required]
        public string HelpExclude { get; set; } = "$^";

        public string HelpSearch { get; set; } = "";

        public override bool Execute()
        {
            var collection = new ProjectCollection();
            var root = ProjectRootElement.Open(HelpProject, collection);
            var docs = new ConcurrentDictionary<string, XDocument>();
            var eval = new Project(root, null, null, collection);
            
            var declaredProps = new HashSet<string>(root.Properties.Select(x => x.Name).Distinct((StringComparer.OrdinalIgnoreCase)));
            var declaredTargets = new HashSet<string>(root.Targets.Select(x => x.Name).Distinct(), StringComparer.OrdinalIgnoreCase);

            var includeExpr = new Regex(HelpInclude, RegexOptions.IgnoreCase);
            var excludeExpr = new Regex(HelpExclude, RegexOptions.IgnoreCase);
            var searchExpr = new Regex(HelpSearch, RegexOptions.IgnoreCase);

            // Should exclude it if it doesn't match the include or matches the exclude
            Predicate<string> shouldExclude = value 
                => (!includeExpr.IsMatch(value) || excludeExpr.IsMatch(value));

            Predicate<string> satisfiesSearch = value 
                => string.IsNullOrWhiteSpace(HelpSearch) || searchExpr.IsMatch(value);

            var helpProperties = bool.Parse(HelpProperties);
            var helpTargets = bool.Parse(HelpTargets);
            var helpImports = bool.Parse(HelpImports);

            var metaHelp = new StringBuilder();
            metaHelp.Append("Help: properties to customize what 'Help' reports");

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

            var hasProps = false;
            if (helpProperties)
            {
                var propsHelp = new StringBuilder();
                propsHelp.Append("Properties:");

                foreach (var prop in eval.Properties
                    .Where(x => x.Xml != null && !x.Name.StartsWith("_"))
                    .OrderBy(x => x.Name))
                {
                    var isMeta = Path.GetFileName(prop.Xml.Location.File) == "CoreBuild.Help.targets";
                    var isLocal = declaredProps.Contains(prop.Name);
                    var builder = isMeta ? metaHelp : propsHelp;

                    // Skip non-meta props that should be excluded
                    if (!isMeta && shouldExclude(prop.Name))
                        continue;

                    // Skip non-meta props that are from imports as needed
                    if (!isMeta && !helpImports && !isLocal)
                        continue;

                    var doc = docs.GetOrAdd(
                        isLocal ? HelpProject : prop.Xml.Location.File,
                        file => XDocument.Load(file, LoadOptions.SetLineInfo));
                    var element = isLocal
                        ? FindElement(doc, prop.Name, root.Properties.First(x => x.Name == prop.Name).Location)
                        : FindElement(doc, prop.Name, prop.Xml.Location);

                    var comment = "";
                    if (element.PreviousNode != null && element.PreviousNode.NodeType == XmlNodeType.Comment)
                        comment = ((XComment)element.PreviousNode).Value;

                    if (isMeta || satisfiesSearch(prop.Name) || satisfiesSearch(comment))
                    {
                        // We got a prop. Flag if non-meta
                        if (!isMeta)
                            hasProps = true;

                        builder.AppendLine().Append($"\t- {prop.Name}");
                        if (!string.IsNullOrWhiteSpace(comment))
                            AppendComment(builder, prop.Name, comment);
                    }
                }

                if (hasProps)
                    help.Append(propsHelp.ToString());
            }

            if (helpTargets)
            {
                var hasTargets = false;
                var targetsHelp = new StringBuilder();
                if (hasProps)
                    targetsHelp.AppendLine().AppendLine();

                targetsHelp.Append("Targets:");
                foreach (var target in eval.Targets
                    .Where(x => x.Key != "Help" && !x.Key.StartsWith("_")).OrderBy(x => x.Key)
                    .Where(x => !shouldExclude(x.Key))
                    // Skip targets that are from imports as needed
                    .Where(x => helpImports ? true : declaredTargets.Contains(x.Key))
                    .OrderBy(x => x.Key))
                {
                    var isLocal = declaredTargets.Contains(target.Key);
                    var doc = docs.GetOrAdd(
                        declaredTargets.Contains(target.Key) ? HelpProject : target.Value.Location.File, 
                        file => XDocument.Load(file, LoadOptions.SetLineInfo));

                    var element = isLocal
                        ? FindElement(doc, "Target", root.Targets.First(x => x.Name == target.Key).Location)
                        : FindElement(doc, "Target", target.Value.Location);
                    var comment = "";
                    if (element.PreviousNode != null && element.PreviousNode.NodeType == XmlNodeType.Comment)
                        comment = ((XComment)element.PreviousNode).Value;

                    if (satisfiesSearch(target.Key) || satisfiesSearch(comment))
                    {
                        hasTargets = true;
                        targetsHelp.AppendLine().Append($"\t- {target.Key}");
                        if (!string.IsNullOrWhiteSpace(comment))
                            AppendComment(targetsHelp, target.Key, comment);
                    }
                }

                if (hasTargets)
                    help.Append(targetsHelp.ToString());
            }

            help.AppendLine();
            metaHelp.AppendLine();
            Log.LogMessage(MessageImportance.High, help.ToString());
            Log.LogMessage(MessageImportance.Normal, metaHelp.ToString());

            return true;
        }

        XElement FindElement(XDocument doc, string elementName, ElementLocation location)
            => (from x in doc.Root.Descendants()
                where x.Name.LocalName == elementName
                let info = (IXmlLineInfo)x
                where info.HasLineInfo() &&
                    info.LineNumber == location.Line &&
                    // LinePosition starts at 1, 0 means no information
                    info.LinePosition == (location.Column + 1)
                select x)
               .First();

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
