using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZeroTrace;

// Embed/Hide File In Image
using var img = Image.Load<Rgba32>("input.png");
var secretFile = File.ReadAllBytes("message.txt");
Steganography.EmbedFileInImage(secretFile, img, "secret_output.png");

// Extract File From Image
using var secretImg = Image.Load<Rgba32>("secret_output.png");
var recoveredBytes = Steganography.ExtractFileFromImage(secretImg);
File.WriteAllBytes("recovered_message.txt", recoveredBytes);