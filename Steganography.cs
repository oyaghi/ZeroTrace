using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ZeroTrace;

public static class Steganography
{
    private const int HeaderSizeInBytes = 4;

    public static void HideFile(byte[] fileData, Image<Rgba32> image, string outputPath)
    {
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
                    var bit = GetBit(payload, bitIndex++);
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

    public static byte[] Decode(Image<Rgba32> image)
    {
        var extractedBytes = ExtractAllPossibleBytes(image);

        // 1. Extract the length from the first 4 bytes
        var header = extractedBytes.Take(HeaderSizeInBytes).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(header);
        }

        var fileLength = BitConverter.ToInt32(header, 0);

        // 2. Return the file data slice
        return extractedBytes
            .Skip(HeaderSizeInBytes)
            .Take(fileLength)
            .ToArray();
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

    private static List<byte> ExtractAllPossibleBytes(Image<Rgba32> image)
    {
        var bytes = new List<byte>();
        byte currentByte = 0;
        var bitCount = 0;

        for (int column = 0; column < image.Height; column++)
        {
            for (int row = 0; row < image.Width; row++)
            {
                var pixel = image[row, column];
                byte[] channels = [pixel.R, pixel.G, pixel.B];

                foreach (var channel in channels)
                {
                    currentByte = (byte)((currentByte << 1) | (channel & 1));
                    bitCount++;

                    if (bitCount == 8)
                    {
                        bytes.Add(currentByte);
                        currentByte = 0;
                        bitCount = 0;
                    }
                }
            }
        }
        return bytes;
    }

    private static int GetBit(IReadOnlyList<byte> data, int bitPosition)
    {
        var byteIndex = bitPosition / 8;
        var bitOffset = 7 - (bitPosition % 8);
        return (data[byteIndex] >> bitOffset) & 1;
    }
}