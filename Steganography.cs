using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ZeroTrace;

public static class Steganography
{
    private const int HeaderSizeInBytes = 4;

    public static void EmbedFileInImage(byte[] fileData, Image<Rgba32> image, string outputPath)
    {
        ValidateRequest(fileData, image);

        var bitIndex = 0;
        var payload = PreparePayload(fileData);
        var totalBits = payload.Length * 8;

        for (int column = 0; column < image.Height && bitIndex < totalBits; column++)
        {
            for (int row = 0; row < image.Width && bitIndex < totalBits; row++)
            {
                var pixel = image[row, column];
                byte[] channels = [pixel.R, pixel.G, pixel.B];

                for (int i = 0; i < channels.Length && bitIndex < totalBits; i++)
                {
                    var bit = GetBitFromPayload(payload, bitIndex++);
                    channels[i] = (byte)((channels[i] & 0xFE) | bit);
                }

                // Map back to the pixel structure
                pixel.R = channels[0];
                pixel.G = channels[1];
                pixel.B = channels[2];

                image[row, column] = pixel;
            }
        }

        image.SaveAsPng(outputPath);
    }

    public static byte[] ExtractFileFromImage(Image<Rgba32> image)
    {
        // Pass 1: extract only the header to determine file length
        var header = ExtractBytesFromImage(image, HeaderSizeInBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(header);
        }

        var fileLength = BitConverter.ToInt32(header, 0);

        if (fileLength <= 0 || fileLength > (image.Width * image.Height * 3 / 8) - HeaderSizeInBytes)
            throw new InvalidOperationException("No valid embedded file found, or data is corrupt.");

        // Pass 2: extract header + file data now that we know the exact size
        var payload = ExtractBytesFromImage(image, HeaderSizeInBytes + fileLength);

        return payload.Skip(HeaderSizeInBytes).ToArray();
    }

    private static byte[] PreparePayload(byte[] fileData)
    {
        var lengthHeader = BitConverter.GetBytes(fileData.Length);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(lengthHeader);
        }

        var payload = new byte[HeaderSizeInBytes + fileData.Length];
        Buffer.BlockCopy(lengthHeader, 0, payload, 0, HeaderSizeInBytes);
        Buffer.BlockCopy(fileData, 0, payload, HeaderSizeInBytes, fileData.Length);

        return payload;
    }

    private static byte[] ExtractBytesFromImage(Image<Rgba32> image, int byteCount)
    {
        var result = new byte[byteCount];
        var bytesWritten = 0;
        byte currentByte = 0;
        var bitCount = 0;

        for (int column = 0; column < image.Height && bytesWritten < byteCount; column++)
        {
            for (int row = 0; row < image.Width && bytesWritten < byteCount; row++)
            {
                var pixel = image[row, column];
                byte[] channels = [pixel.R, pixel.G, pixel.B];

                foreach (var channel in channels)
                {
                    currentByte = (byte)((currentByte << 1) | (channel & 1));
                    bitCount++;

                    if (bitCount == 8)
                    {
                        result[bytesWritten++] = currentByte;
                        currentByte = 0;
                        bitCount = 0;
                    }
                }
            }
        }

        return result;
    }

    private static int GetBitFromPayload(IReadOnlyList<byte> data, int bitPosition)
    {
        var byteIndex = bitPosition / 8;
        var bitOffset = 7 - (bitPosition % 8);

        return (data[byteIndex] >> bitOffset) & 1;
    }

    private static void ValidateRequest(IReadOnlyCollection<byte> fileData, Image image)
    {
        var availableBits = (long)image.Width * image.Height * 3;
        var requiredBits = (long)(HeaderSizeInBytes + fileData.Count) * 8;
        if (requiredBits > availableBits)
        {
            throw new InvalidOperationException($"The image is too small! Required: {requiredBits} bits, but only {availableBits} are available.");
        }
    }
}