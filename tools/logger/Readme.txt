CoreBuild.ColoredConsoleLogger:
=========================================

Provides support for colored messages with the format `{message:color}`, 
where `color` can either be a named color that can be parsed by 
`System.Drawing.Color.FromName(string)` or a hex color starting with 
`#` that can be converted to a `Color` by 
`System.Drawing.ColorTranslator.FromHtml(string)`.

Usage:

    msbuild /logger:ColoredConsoleLogger,CoreBuild.ColoredConsoleLogger.dll /noconsolelogger

Examples:

```
  <Message Text="{Hello:Aqua} {world:#7FFF00}!" Importance="high" />
```