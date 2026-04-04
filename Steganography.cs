using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ZeroTrace;

public static class Steganography
{
    private const int HeaderSizeInBytes = 4;

    public static Result<bool> EmbedFileInImage(byte[] fileData, Image<Rgba32> image, string outputPath)
    {
        var validationMessage = ValidateRequest(fileData, image);
        if (validationMessage is not null)
        {
            return new() { IsSuccess = false, Message = validationMessage};
        }

        var bitIndex = 0;
        var payload = PreparePayload(fileData);
        var totalBits = payload.Length * 8;

        for (int row = 0; row < image.Height && bitIndex < totalBits; row++)
        {
            for (int column = 0; column < image.Width && bitIndex < totalBits; column++)
            {
                var pixel = image[column, row];
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

                image[column, row] = pixel;
            }
        }

        image.SaveAsPng(outputPath);

        return new() { IsSuccess = true, Message = "File embedded successfully!" };
    }

    public static Result<byte[]> ExtractFileFromImage(Image<Rgba32> image)
    {
        // Pass 1: extract only the header to determine file length
        var header = ExtractBytesFromImage(image, HeaderSizeInBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(header);
        }

        var fileLength = BitConverter.ToInt32(header, 0);
        if (fileLength <= 0 || fileLength > (image.Width * image.Height * 3 / 8) - HeaderSizeInBytes)
        {
            return new(){IsSuccess = false, Message = "No valid embedded file found, or data is corrupt."};
        }

        // Pass 2: extract header + file data now that we know the exact size
        var payload = ExtractBytesFromImage(image, HeaderSizeInBytes + fileLength);

        return new(){IsSuccess = true, Value = payload.Skip(HeaderSizeInBytes).ToArray()};
    }

    // combine the fileLength (header) and the fileData
    private static byte[] PreparePayload(byte[] fileData)
    {
        var lengthHeader = BitConverter.GetBytes(fileData.Length);
        // reverse the byte order if the system is little endian
        // since the file length is stored in network byte order (big endian)
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

        for (int row = 0; row < image.Height && bytesWritten < byteCount; row++)
        {
            for (int column = 0; column < image.Width && bytesWritten < byteCount; column++)
            {
                var pixel = image[column, row];
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
        // finding the byte that our bit belongs to and bit offset
        var byteIndex = bitPosition / 8;
        var bitOffset = 7 - (bitPosition % 8);

        // masking the output leaving only the last bit of the byte
        return (data[byteIndex] >> bitOffset) & 1;
    }

    private static string? ValidateRequest(IReadOnlyCollection<byte> fileData, Image image)
    {
        var availableBits = (long)image.Width * image.Height * 3;
        var requiredBits = (long)(HeaderSizeInBytes + fileData.Count) * 8;

        return requiredBits > availableBits ? $"The image is too small! Required: {requiredBits} bits, but only {availableBits} are available." : null;
    }
}