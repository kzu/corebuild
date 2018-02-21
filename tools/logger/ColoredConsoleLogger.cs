using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colorful;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace CoreBuild
{
    /// <summary>
    /// Provides support for colored messages with the format <c>{message:color}</c>', 
    /// where <c>color</c> can either be a named color that can be parsed by 
    /// <see cref="Color.FromName(string)"/> or a hex color starting with <c>#</c> that 
    /// can be converted by <see cref="ColorTranslator.FromHtml(string)"/>.
    /// </summary>
    public class ColoredConsoleLogger : ConsoleLogger
    {
        static readonly Regex text = new Regex(@"(?<colored>\{(?<text>[^\{\}]+?)\:(?<color>[^\{\}]+?)(?<background>,[^\{\}]+?)?\})|(?<regular>[^\#\{]+)", RegexOptions.Compiled | RegexOptions.Singleline);

        StyleSheet style = new StyleSheet(Console.ForegroundColor);

        public ColoredConsoleLogger()
        {
            WriteHandler = Write;
        }

        public ColoredConsoleLogger(LoggerVerbosity verbosity)
            : base(verbosity)
        {
            WriteHandler = Write;
        }

        public override void Initialize(IEventSource eventSource)
        {
            base.Initialize(eventSource);
            ApplyCss();
        }

        public override void Initialize(IEventSource eventSource, int nodeCount)
        {
            base.Initialize(eventSource, nodeCount);
            ApplyCss();
        }

        private void ApplyCss()
        {
            var css = "";

            if (!string.IsNullOrEmpty(Parameters))
            {
                foreach (var parameter in Parameters.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    if (parameter.EndsWith(".css"))
                    {
                        css = parameter;
                        if (css.StartsWith("css=", System.StringComparison.OrdinalIgnoreCase))
                            css = css.Substring("css=".Length);

                        css = css.Trim('"');
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(css))
                css = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.css").FirstOrDefault();

            ParseStyles(css);
        }

        void ParseStyles(string file)
        {
            if (string.IsNullOrEmpty(file))
                return;

            if (!File.Exists(file))
                throw new FileNotFoundException($"Specified CSS file '{file}' was not found.", file);

            var css = new ExCSS.Parser().Parse(File.ReadAllText(file));

            Console.Write($"Reading CSS styles from '{file}'...");
            Console.Write(System.Environment.NewLine);

            foreach (var rule in css.StyleRules)
            {
                var match = ((ExCSS.PrimitiveTerm)rule.Declarations.Properties.Find(p => p.Name == "-match")?.Term)?.Value as string;
                var color = ((ExCSS.PrimitiveTerm)rule.Declarations.Properties.Find(p => p.Name == "color")?.Term)?.Value as string;
                if (match != null && color != null)
                    style.AddStyle(match, GetColor(color));
            }
        }

        void Write(string value)
        {
            if (value.IndexOf('{') == -1 || value.IndexOf('}') == -1)
            {
                Console.WriteStyled(value, style);
                return;
            }

            foreach (Match match in text.Matches(value))
            {
                Console.WriteStyled(match.Groups["regular"].Value, style);
                if (match.Groups["colored"].Success)
                {
                    var text = match.Groups["text"].Value;
                    var color = GetColor(match.Groups["color"].Value);
                    var bg = GetColor(match.Groups["background"].Value.Replace(",", ""));

                    try
                    {
                        if (bg != Color.Empty)
                            Console.BackgroundColor = bg;

                        Console.Write(text, color);
                    }
                    finally
                    {
                        System.Console.ResetColor();
                        Console.ResetColor();
                    }
                }
            }
        }

        Color GetColor(string color) =>
            string.IsNullOrEmpty(color) ? Color.Empty :
            color.StartsWith("#") ? ColorTranslator.FromHtml(color) : Color.FromName(color);
    }
}
