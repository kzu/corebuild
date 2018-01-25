using System;
using System.Drawing;
using System.Text.RegularExpressions;
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
        static readonly Regex regex = new Regex(@"(?<colored>\{(?<text>[^\{\}]+?)\:(?<color>[^\{\}]+?)\})|(?<regular>.+?)", RegexOptions.Compiled | RegexOptions.Singleline);

        public ColoredConsoleLogger()
            : base()
        {
            WriteHandler = Write;
        }

        public ColoredConsoleLogger(LoggerVerbosity verbosity)
            : base(verbosity)
        {
            WriteHandler = Write;
        }

        void Write(string value)
        {
            if (value.IndexOf('{') == -1 || value.IndexOf('}') == -1)
            {
                Console.Write(value);
                return;
            }

            foreach (Match match in regex.Matches(value))
            {
                Console.Write(match.Groups["regular"].Value);
                if (match.Groups["colored"].Success)
                {
                    var text = match.Groups["text"].Value;
                    var color = match.Groups["color"].Value;
                    if (color.StartsWith("#"))
                        Colorful.Console.Write(text, ColorTranslator.FromHtml(color));
                    else
                        Colorful.Console.Write(text, Color.FromName(color));
                }
            }
        }
    }
}
