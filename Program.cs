using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using System.Text;
using ZeroTrace;
using Color = Spectre.Console.Color;

// 1. App Loop: Keep the app running until the user chooses "Exit"
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

    // Pause so the user can see the result before the screen clears
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
    Console.ReadKey(true);
}

static void HandleEmbedFlow()
{
    // 1. Get the Secret Data
    var dataType = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What are you hiding?")
            .AddChoices("Plain Text", "File"));

    byte[] dataToHide;
    if (dataType == "Plain Text")
    {
        dataToHide = Encoding.UTF8.GetBytes(AnsiConsole.Ask<string>("Enter your [green]secret message[/]:"));
    }
    else
    {
        var path = GetFileOrPath("Select the [blue]Secret File[/]:", ["*.*"]);
        if (path == "BACK")
        {
            return;
        }

        dataToHide = File.ReadAllBytes(path);
    }

    // 2. Get the Container Image (Filtered to PNG/JPG)
    string imagePath = GetFileOrPath("Select your [yellow]Container Image[/]:", [".png", ".txt"]); // You can use "*.png" here
    if (imagePath == "BACK") return;

    // 3. Process
    AnsiConsole.Status().Start("Embedding...", _ => {
        using var image = Image.Load<Rgba32>(imagePath);
        Steganography.EmbedFileInImage(dataToHide, image, "output.png");
    });

    AnsiConsole.MarkupLine("[bold green]Success![/] Check [underline]output.png[/]");
}
static void HandleExtractFlow()
{
    string imagePath = GetValidatedPath("Select [yellow]Encoded Image[/]:");

    byte[]? hiddenData = null;
    AnsiConsole.Status().Start("Extracting payload...", _ =>
    {
        using var image = Image.Load<Rgba32>(imagePath);
        hiddenData = Steganography.ExtractFileFromImage(image);
    });

    if (hiddenData == null || hiddenData.Length == 0)
    {
        AnsiConsole.MarkupLine("[red]No data found or extraction failed.[/]");
        return;
    }

    var viewMode = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Data recovered! How should I display it?")
            .AddChoices("Show as Text", "Save as File"));

    if (viewMode == "Show as Text")
    {
        string msg = Encoding.UTF8.GetString(hiddenData);
        AnsiConsole.Write(new Panel(msg).Header("Recovered Message").BorderColor(Color.Green));
    }
    else
    {
        string fileName = AnsiConsole.Ask<string>("Enter filename to save (e.g., recovered.zip):");
        File.WriteAllBytes(fileName, hiddenData);
        AnsiConsole.MarkupLine($"[green]File saved as {fileName}[/]");
    }
}

// Helper method to ensure paths are correct and files exist
static string GetValidatedPath(string prompt)
{
    return AnsiConsole.Prompt(
        new TextPrompt<string>(prompt)
            .Validate(path =>
            {
                string cleaned = path.Trim('"');
                return File.Exists(cleaned)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]File not found![/] Please try again.");
            })).Trim('"');
}

static string GetFileOrPath(string promptTitle, string[] searchPattern)
{
    // 1. Get files and sort them alphabetically
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

    // 2. Build the choices: Actions FIRST, then the files
    var choices = new List<string>
    {
        "[red]-- Go Back --[/]",
        "[yellow]-- Enter path manually / Drag & Drop --[/]"
    };
    choices.AddRange(files!);

    var prompt = new SelectionPrompt<string>()
        .Title(promptTitle)
        .PageSize(10)
        .EnableSearch()
        .SearchPlaceholderText("[grey]Type to filter files...[/]")
        .MoreChoicesText("[grey](Move up and down to reveal more files)[/]")
        .AddChoices(choices);

    prompt.SearchHighlightStyle = new Style(foreground: Color.SpringGreen3);

    var selection = AnsiConsole.Prompt(prompt);
    if (selection == "[red]-- Go Back --[/]")
    {
        return "BACK";
    }

    if (selection == "[yellow]-- Enter path manually / Drag & Drop --[/]")
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Paste path or drag file here:")
                .Validate(path => File.Exists(path.Trim('"'))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]File not found![/]"))
        ).Trim('"');
    }

    return Path.GetFullPath(selection);
}