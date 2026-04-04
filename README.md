# ZeroTrace

A command-line steganography tool for hiding and recovering secret data inside images, using the Least Significant Bit (LSB) technique.

## What it does

ZeroTrace lets you embed any file or text message invisibly into a PNG, BMP, TIFF, or TGA image by overwriting the least significant bit of each RGB channel per pixel. The modification is imperceptible to the human eye. The hidden data can later be extracted from the image to recover the original content.

## Installation

Requires [.NET 10](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/your-username/ZeroTrace
cd ZeroTrace/ZeroTrace
dotnet pack
dotnet tool install --global --add-source ./bin/Release ZeroTrace
```

Once installed, launch from anywhere with:

```bash
zt
```

To uninstall:

```bash
dotnet tool uninstall --global ZeroTrace
```

## Usage

Running `zt` opens an interactive menu with two modes.

### Embed (Hide)

Hides a secret message or file inside a container image.

1. Choose between **Plain Text** or **File**
2. Select or provide the secret content
3. Select a container image (PNG, BMP, TIFF, or TGA)
4. Enter an output filename (defaults to `output.png`)

The result is saved as a PNG. The container image is not modified.

### Extract (Reveal)

Recovers hidden data from a previously encoded image.

1. Select the encoded image
2. Choose to display the result as **text** or **save it as a file**

## How it works

ZeroTrace uses LSB steganography across the R, G, and B channels of each pixel:

- A 4-byte header encoding the payload length is prepended to the data
- Each bit of the combined payload overwrites the LSB of one channel
- A 1920×1080 image can hold up to ~777 KB of hidden data
- Only lossless formats are supported — JPEG is not, as its compression destroys the embedded bits

## Supported formats

| Format | Embed | Extract |
|--------|-------|---------|
| PNG    | ✅    | ✅      |
| BMP    | ✅    | ✅      |
| TIFF   | ✅    | ✅      |
| TGA    | ✅    | ✅      |
| JPEG   | ❌    | ❌      |

## Dependencies

- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) — image loading and pixel manipulation
- [Spectre.Console](https://spectreconsole.net/) — interactive terminal UI
