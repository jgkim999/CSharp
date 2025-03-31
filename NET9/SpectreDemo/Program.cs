// See https://aka.ms/new-console-template for more information

using Spectre.Console;

Console.WriteLine("Hello, World!");
AnsiConsole.Markup("[underline red]Hello[/] World!");
AnsiConsole.WriteLine();
AnsiConsole.Write(new Markup("[bold yellow]Hello[/] [red]World![/]"));
AnsiConsole.WriteLine();
var table = new Table();
table.AddColumn(new TableColumn(new Markup("[yellow]Foo[/]")));
table.AddColumn(new TableColumn("[blue]Bar[/]"));
AnsiConsole.Write(table);