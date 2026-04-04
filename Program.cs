using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using System.Text;
using ZeroTrace;
using Color = Spectre.Console.Color;

var keepRunning = true;
while (keepRunning)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("ZeroTrace").Centered().Color(Color.SpringGreen3));
    AnsiConsole.Write(new Rule("[grey]v1.0.0[/]").Centered());

    var mode = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices("Embed (Hide)", "Extract (Reveal)", "Exit"));

    switch (mode)
    {
        case "Embed (Hide)":
            HandleEmbedFlow();
            break;
        case "Extract (Reveal)":
            HandleExtractFlow();
            break;
        case "Exit":
            keepRunning = false;
            continue;
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
    Console.ReadKey(true);
}

return;

static void HandleEmbedFlow()
{
    var dataType = AnsiConsole.Prompt
    (
        new SelectionPrompt<string>().Title("What are you hiding?").AddChoices("Plain Text", "File")
    );

    byte[] dataToHide;
    if (dataType == "Plain Text")
    {
        dataToHide = Encoding.UTF8.GetBytes(AnsiConsole.Ask<string>("Enter your [green]secret message[/]:"));
    }
    else
    {
        var filePath = GetFileOrPath("Select the [blue]Secret File[/]:", ["*.*"]);
        if (filePath == "BACK")
        {
            return;
        }

        dataToHide = File.ReadAllBytes(filePath);
    }

    var imagePath = GetFileOrPath("Select your [yellow]Container Image[/]:", [".png", ".bmp", ".tiff", ".tga"]);
    if (imagePath == "BACK")
    {
        return;
    }

    AnsiConsole.Status().Start("Embedding...", _ =>
    {
        using var image = Image.Load<Rgba32>(imagePath);

        var result = Steganography.EmbedFileInImage(dataToHide, image, "output.png");
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold green]Success![/] Check [underline]output.png[/]");
    });
}

static void HandleExtractFlow()
{
    var imagePath = GetFileOrPath("Select [yellow]Encoded Image[/]:", [".png", ".bmp", ".tiff", ".tga"]);
    if (imagePath == "BACK")
    {
        return;
    }

    byte[]? hiddenData = null;
    AnsiConsole.Status().Start("Extracting payload...", _ =>
    {
        using var image = Image.Load<Rgba32>(imagePath);

        var result = Steganography.ExtractFileFromImage(image);
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");

            return;
        }

        hiddenData = result.Value;
    });

    if (hiddenData == null || hiddenData.Length == 0)
    {
        return;
    }

    var viewMode = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Data recovered! How should I display it?")
            .AddChoices("Show as Text", "Save as File"));

    if (viewMode == "Show as Text")
    {
        var msg = Encoding.UTF8.GetString(hiddenData);
        AnsiConsole.Write(new Panel(msg).Header("Recovered Message").BorderColor(Color.Green));
    }
    else
    {
        var fileName = AnsiConsole.Ask<string>("Enter filename to save (e.g., recovered.zip):");
        File.WriteAllBytes(fileName, hiddenData);

        AnsiConsole.MarkupLine($"[green]File saved as {fileName}[/]");
    }
}

static string GetFileOrPath(string promptTitle, string[] searchPattern)
{
    var files = Directory.GetFiles(Directory.GetCurrentDirectory())
        .Select(Path.GetFileName)
        .Where(file =>
        {
            if (searchPattern.Contains("*.*"))
            {
                return true;
            }

            var ext = Path.GetExtension(file)?.ToLower();
            return searchPattern.Contains(ext);
        })
        .OrderBy(f => f)
        .ToList();

    var choices = new List<string>
    {
        "[red]-- Go Back --[/]",
        "[yellow]-- Enter path manually / Drag & Drop --[/]"
    };

    choices.AddRange(files!);
    var prompt = new SelectionPrompt<string>()
        .PageSize(10)
        .EnableSearch()
        .Title(promptTitle)
        .AddChoices(choices)
        .SearchPlaceholderText("[grey]Type to filter files...[/]")
        .MoreChoicesText("[grey](Move up and down to reveal more files)[/]");

    prompt.SearchHighlightStyle = new Style(foreground: Color.SpringGreen3);

    var selection = AnsiConsole.Prompt(prompt);
    if (selection == "[red]-- Go Back --[/]")
    {
        return "BACK";
    }

    if (selection == "[yellow]-- Enter path manually / Drag & Drop --[/]")
    {
        return AnsiConsole.Prompt
        (
            new TextPrompt<string>("Paste path or drag file here:")
                .Validate(path => File.Exists(path.Trim('"'))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]File not found![/]"))
        ).Trim('"');
    }

    return Path.GetFullPath(selection);
}