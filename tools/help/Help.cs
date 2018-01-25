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

namespace CoreBuild
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

        /// <summary>
        /// Explicit properties to always include in documentation, 
        /// regardless of whether they are from imported targets.
        /// </summary>
        public ITaskItem[] HelpProperty { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Explicit targets to always include in documentation, 
        /// regardless of whether they are from imported targets.
        /// </summary>
        public ITaskItem[] HelpTarget { get; set; } = new ITaskItem[0];

        public string HelpSearch { get; set; } = "";

        public override bool Execute()
        {
            var collection = new ProjectCollection();
            var xml = new ConcurrentDictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
            var root = ProjectRootElement.Open(HelpProject, collection);
            var eval = new Project(root, null, null, collection);

            var logical = eval.GetLogicalProject().ToArray();

            var allProps = logical
                .OfType<ProjectPropertyElement>()
                // Add the local props first
                .Where(x => x.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase))
                .Concat(logical
                .OfType<ProjectPropertyElement>()
                .Where(x => !x.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            var allTargets = logical
                .OfType<ProjectTargetElement>()
                // Add the local props first
                .Where(x => x.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase))
                .Concat(logical
                .OfType<ProjectTargetElement>()
                .Where(x => !x.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            var includeExpr = new Regex(HelpInclude, RegexOptions.IgnoreCase);
            var excludeExpr = new Regex(HelpExclude, RegexOptions.IgnoreCase);
            var searchExpr = new Regex(HelpSearch, RegexOptions.IgnoreCase);

            // Should exclude it if it doesn't match the include or matches the exclude
            bool ShouldExclude(string value)
                => (!includeExpr.IsMatch(value) || excludeExpr.IsMatch(value));

            bool SatisfiesSearch(string value)
                => string.IsNullOrWhiteSpace(HelpSearch) || searchExpr.IsMatch(value);

            var helpProperties = bool.Parse(HelpProperties);
            var helpTargets = bool.Parse(HelpTargets);
            var helpImports = bool.Parse(HelpImports);

            var metaHelp = new StringBuilder();
            metaHelp.Append("Help: properties to customize what 'Help' reports");

            var help = new StringBuilder();
            var standard = 
                allTargets.Any(t => t.Name == "Configure") &&
                allTargets.Any(t => t.Name == "Build") &&
                allTargets.Any(t => t.Name == "Test") &&
                allTargets.Any(t => t.Name == "Run");

            if (standard)
            {
                help.AppendLine("Standard: {YES:LawnGreen} √ (Configure, Build, Test and Run targets supported)").AppendLine();
            }
            else
            {
                help.AppendLine("Standard: {NO:Tomato} x (Missing Configure, Build, Test or Run targets. Lean more at http://corebuild.io").AppendLine();
                Log.LogWarning(null, "CB01", null, null, 0, 0, 0, 0, "This project is NOT CoreBuild Standard compatible. Please provide Configure, Build, Test and Run targets. Lean more at http://corebuild.io");
            }

            var hasProps = false;
            if (helpProperties)
            {
                var propsHelp = new StringBuilder();
                propsHelp.Append("Properties:");
                var processed = new HashSet<string>();
                var alwaysInclude = new HashSet<string>(HelpProperty.Select(x => x.ItemSpec));
                
                foreach (var prop in allProps
                    .Where(x => x != null && !x.Name.StartsWith("_"))
                    .OrderBy(x => x.Name))
                {
                    // First property to make it to the list wins. 
                    // Target project source is loaded first, followed by imported properties.
                    if (processed.Contains(prop.Name))
                        continue;

                    var isMeta = Path.GetFileName(prop.Location.File) == "CoreBuild.Help.targets";
                    var builder = isMeta ? metaHelp : propsHelp;

                    if (!alwaysInclude.Contains(prop.Name))
                    {
                        // Skip non-meta props that should be excluded
                        if (!isMeta && ShouldExclude(prop.Name))
                        {
                            processed.Add(prop.Name);
                            continue;
                        }

                        var isLocal = prop.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase);
                        // Skip non-meta props that are from imports as needed
                        if (!isMeta && !helpImports && !isLocal)
                        {
                            processed.Add(prop.Name);
                            continue;
                        }
                    }

                    var candidate = new CandidateElement(prop.Name, allProps.Where(x => x.Name == prop.Name), xml);
                    if (candidate.IsHidden)
                    {
                        processed.Add(prop.Name);
                        continue;
                    }

                    if (isMeta || alwaysInclude.Contains(prop.Name) || SatisfiesSearch(prop.Name) || SatisfiesSearch(candidate.Comment))
                    {
                        // We got a prop. Flag if non-meta
                        if (!isMeta)
                            hasProps = true;

                        if (isMeta)
                            builder.AppendLine().Append($"\t- {prop.Name}");
                        else
                            builder.AppendLine().Append($"\t- {{{prop.Name}:Aqua}}");

                        if (!string.IsNullOrWhiteSpace(candidate.Comment))
                            AppendComment(builder, prop.Name, candidate.Comment);

                        processed.Add(prop.Name);
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
                var processed = new HashSet<string>();
                var alwaysInclude = new HashSet<string>(HelpTarget.Select(x => x.ItemSpec));

                foreach (var target in allTargets
                    .Where(x => x.Name != "Help" && !x.Name.StartsWith("_"))
                    .OrderBy(x => x.Name))
                {
                    // First target to make it to the list wins. 
                    // Target project source is loaded first, followed by imported targets.
                    if (processed.Contains(target.Name))
                        continue;

                    if (!alwaysInclude.Contains(target.Name))
                    {
                        var isLocal = target.Location.File.Equals(HelpProject, StringComparison.OrdinalIgnoreCase);
                        // Skip targets that should be excluded
                        if (ShouldExclude(target.Name) ||
                            // Skip targets that are from imports as needed
                            (!helpImports && !isLocal))
                        {
                            processed.Add(target.Name);
                            continue;
                        }
                    }

                    var candidate = new CandidateElement(target.Name, allTargets.Where(x => x.Name == target.Name), xml);
                    if (candidate.IsHidden)
                    {
                        processed.Add(target.Name);
                        continue;
                    }

                    if (alwaysInclude.Contains(target.Name) || SatisfiesSearch(target.Name) || SatisfiesSearch(candidate.Comment))
                    {
                        hasTargets = true;
                        targetsHelp.AppendLine().Append($"\t- {{{target.Name}:Yellow}}");
                        if (!string.IsNullOrWhiteSpace(candidate.Comment))
                            AppendComment(targetsHelp, target.Name, candidate.Comment);
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

        static XElement GetXml(XDocument doc, ElementLocation location)
            => (from x in doc.Root.Descendants()
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

        class CandidateElement
        {
            string name;
            IEnumerable<ProjectElement> elements;
            ConcurrentDictionary<string, XDocument> xmlDocs;

            public CandidateElement(string name, IEnumerable<ProjectElement> elements, ConcurrentDictionary<string, XDocument> xmlDocs)
            {
                this.name = name;
                this.elements = elements;
                this.xmlDocs = xmlDocs;

                // Allows hiding via the Label attribute.
                IsHidden = elements.Any(e =>
                    e.Label.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
                    e.Parent.Label.Equals("hidden", StringComparison.OrdinalIgnoreCase));

                // Get the first with a comment? The one with the longest comment?
                var comments = elements
                    .Select(e => GetXml(
                            xmlDocs.GetOrAdd(e.Location.File, file => XDocument.Load(file, LoadOptions.SetLineInfo)),
                            e.Location))
                    .Where(e => e.PreviousNode?.NodeType == XmlNodeType.Comment)
                    .Select(e => ((XComment)e.PreviousNode).Value.Trim())
                    .Where(c => !string.IsNullOrEmpty(c));

                Comment = string.Join(Environment.NewLine, comments);

                if (!IsHidden)
                    // Also allow hiding specific properties or targets with an @hidden annotation via a comment.
                    IsHidden = Comment.IndexOf("@hidden", StringComparison.OrdinalIgnoreCase) != -1;
            }

            public bool IsHidden { get; }

            public string Comment { get; }
        }
    }
}
