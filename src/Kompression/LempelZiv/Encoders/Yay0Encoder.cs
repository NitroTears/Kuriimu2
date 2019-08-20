﻿using System.IO;
using System.Text;
using Kompression.IO;

namespace Kompression.LempelZiv.Encoders
{
    public class Yay0Encoder : ILzEncoder
    {
        private readonly ByteOrder _byteOrder;

        public Yay0Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output, LzMatch[] matches)
        {
            var bitLayoutStream = new MemoryStream();
            var compressedTableStream = new MemoryStream();
            var uncompressedTableStream = new MemoryStream();

            using (var bitLayoutWriter = new BitWriter(bitLayoutStream, BitOrder.MSBFirst, 1, ByteOrder.BigEndian))
            using (var bwCompressed = new BinaryWriter(compressedTableStream))
            using (var bwUncompressed = new BinaryWriter(uncompressedTableStream))
            {
                foreach (var match in matches)
                {
                    // Write any data before the match, to the uncompressed table
                    while (input.Position < match.Position)
                    {
                        bitLayoutWriter.WriteBit(1);
                        bwUncompressed.Write((byte)input.ReadByte());
                    }

                    // Write match data to the compressed table
                    var firstByte = (byte)((match.Displacement - 1) >> 8);
                    var secondByte = (byte)(match.Displacement - 1);

                    if (match.Length < 0x12)
                        // Since minimum length should be 3 for Yay0, we get a minimum matchLength of 1 in this case
                        firstByte |= (byte)((match.Length - 2) << 4);
                    else
                        // Yes, we do write the length for a match into the uncompressed data stream, if it's >=0x12
                        bwUncompressed.Write((byte)(match.Length - 0x12));

                    bitLayoutWriter.WriteBit(0);
                    bwCompressed.Write(firstByte);
                    bwCompressed.Write(secondByte);

                    input.Position += match.Length;
                }

                // Write any data after last match, to the uncompressed table
                while (input.Position < input.Length)
                {
                    bitLayoutWriter.WriteBit(1);
                    bwUncompressed.Write((byte)input.ReadByte());
                }

                bitLayoutWriter.Flush();
            }

            WriteCompressedData(input, output, bitLayoutStream, compressedTableStream, uncompressedTableStream);
        }

        private void WriteCompressedData(Stream input, Stream output, Stream bitLayoutStream, Stream compressedTableStream, Stream uncompressedTableStream)
        {
            // Create header values
            var uncompressedLength = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian((int)input.Length)
                : GetBigEndian((int)input.Length);
            var compressedTableOffsetInt = (int)(0x10 + ((bitLayoutStream.Length + 3) & ~3));
            var compressedTableOffset = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian(compressedTableOffsetInt)
                : GetBigEndian(compressedTableOffsetInt);
            var uncompressedTableOffset = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian((int)(compressedTableOffsetInt + compressedTableStream.Length))
                : GetBigEndian((int)(compressedTableOffsetInt + compressedTableStream.Length));

            // Write header
            output.Write(Encoding.ASCII.GetBytes("Yay0"), 0, 4);
            output.Write(uncompressedLength, 0, 4);
            output.Write(compressedTableOffset, 0, 4);
            output.Write(uncompressedTableOffset, 0, 4);

            // Write data streams
            bitLayoutStream.Position = 0;
            bitLayoutStream.CopyTo(output);
            output.Position = (output.Position + 3) & ~3;

            compressedTableStream.Position = 0;
            compressedTableStream.CopyTo(output);

            uncompressedTableStream.Position = 0;
            uncompressedTableStream.CopyTo(output);
        }

        private byte[] GetLittleEndian(int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        private byte[] GetBigEndian(int value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}