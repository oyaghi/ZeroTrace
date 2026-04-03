using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZeroTrace;

// Hiding
using var img = Image.Load<Rgba32>("input.png");
var secretFile = File.ReadAllBytes("message.txt");
Steganography.HideFile(secretFile, img, "secret_output.png");

// Decoding
using var secretImg = Image.Load<Rgba32>("secret_output.png");
var recoveredBytes = Steganography.Decode(secretImg);
File.WriteAllBytes("recovered_message.txt", recoveredBytes);