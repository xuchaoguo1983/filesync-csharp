using System;
using System.Collections.Generic;
using System.IO;

namespace filesync
{
	public class Rdiff
	{
		public const int SIG_MAGIC = 0x72730136;

		public const int DELTA_MAGIC = 0x72730236;

		public const short CHAR_OFFSET = 31;

		public const byte OP_END = 0x00;

		public const byte OP_LITERAL_N1 = 0x41;
		public const byte OP_LITERAL_N2 = 0x42;
		public const byte OP_LITERAL_N4 = 0x43;
		public const byte OP_LITERAL_N8 = 0x44;

		public const byte OP_COPY_N4_N4 = 0x4f;

		private Configuration config;

		// Constructors.
		// -----------------------------------------------------------------
		public Rdiff (Configuration c)
		{
			config = c;
		}

		/// <summary>
		/// Make the signatures from data coming in through the input stream.
		/// </summary>
		/// <returns>The signatures.</returns>
		/// <param name="filePath">File path.</param>
		public List<ChecksumPair> makeSignatures (String filePath)
		{
			return new Generator (config).generateSums (filePath);
		}

		/// <summary>
		/// Write the signatures to the specified output stream
		/// </summary>
		/// <param name="pairs">Pairs.</param>
		/// <param name="filePath">File path.</param>
		public void writeSignatures (List<ChecksumPair> pairs, Stream stream)
		{
			writeInt (SIG_MAGIC, stream);
			writeInt (config.blockLength, stream);
			writeInt (config.strongSumLength, stream);

			foreach (ChecksumPair pair in pairs) {
				writeInt (pair.Weak, stream);
				stream.Write (pair.Strong.Bytes, 0, config.strongSumLength);
			}
		}

		/// <summary>
		/// Writes the signatures to the specified file path.
		/// </summary>
		/// <param name="pairs">Pairs.</param>
		/// <param name="filePath">File path.</param>
		public void writeSignatures(List<ChecksumPair> pairs, String filePath) {
			using (FileStream fs = new FileStream (filePath, FileMode.OpenOrCreate)) {
				writeSignatures (pairs, fs);
			}
		}

		/// <summary>
		/// Read the signatures from the input stream.
		/// </summary>
		/// <returns>The signatures.</returns>
		/// <param name="filePath">File path.</param>
		public List<ChecksumPair> readSignatures (Stream stream)
		{
			using (StreamReader sr = new StreamReader (stream)) {
				List<ChecksumPair> pairs = new List<ChecksumPair> ();

				int header = readInt (stream);
				if (header != SIG_MAGIC) {
					throw new IOException ("Bad signature header:" + header.ToString ("X"));
				}

				long off = 0;
				config.blockLength = readInt (stream);
				config.strongSumLength = readInt (stream);

				int weak;
				byte[] strong = new byte[config.strongSumLength];
				do {
					try {
						weak = readInt (stream);
						int len = stream.Read (strong, 0, strong.Length);
						if (len < config.strongSumLength)
							break;
						pairs.Add (new ChecksumPair (off, weak, strong));
						off += config.blockLength;
					} catch (EndOfStreamException eof) {
						break;
					}
				} while (true);

				return pairs;
			}
		}

		/// <summary>
		/// Make a collection of {@link Delta}s from the given sums and input stream.
		/// </summary>
		/// <returns>The deltas.</returns>
		/// <param name="pairs">Pairs.</param>
		/// <param name="stream">Stream.</param>
		public List<Delta> makeDeltas (List<ChecksumPair> pairs, Stream stream)
		{
			return new Matcher (config).hashSearch (pairs, stream);
		}

		/// <summary>
		/// Write deltas to an output stream.
		/// </summary>
		/// <param name="deltas">Deltas.</param>
		/// <param name="stream">Stream.</param>
		public void writeDeltas (List<Delta> deltas, Stream stream)
		{
			writeInt (DELTA_MAGIC, stream);

			foreach (Delta o in deltas) {
				if (o is Offsets) {
					writeCopy ((Offsets)o, stream);
				} else {
					writeLiteral ((DataBlock)o, stream);
				}
			}

			stream.WriteByte (OP_END);
		}

		/// <summary>
		/// Read a collection of {@link Delta}s from the input stream.
		/// </summary>
		/// <returns>The deltas.</returns>
		/// <param name="stream">Stream.</param>
		public List<Delta> readDeltas (Stream stream)
		{
			List<Delta> deltas = new List<Delta> ();

			int header = readInt (stream);
			if (header != DELTA_MAGIC) {
				throw new IOException ("Bad delta header: 0x"
				+ header.ToString ("X"));
			}
			int command;
			long offset = 0;
			byte[] buf;
			while ((command = stream.ReadByte ()) != -1) {
				switch (command) {
				case OP_END:
					return deltas;
				case OP_LITERAL_N1:
					buf = new byte[(int)readInt (1, stream)];
					stream.Read (buf, 0, buf.Length);
					deltas.Add (new DataBlock (offset, buf));
					offset += buf.Length;
					break;
				case OP_LITERAL_N2:
					buf = new byte[(int)readInt (2, stream)];
					stream.Read (buf, 0, buf.Length);
					deltas.Add (new DataBlock (offset, buf));
					offset += buf.Length;
					break;
				case OP_LITERAL_N4:
					buf = new byte[(int)readInt (4, stream)];
					stream.Read (buf, 0, buf.Length);
					deltas.Add (new DataBlock (offset, buf));
					offset += buf.Length;
					break;
				case OP_COPY_N4_N4:
					int oldOff = (int)readInt (4, stream);
					int bs = (int)readInt (4, stream);
					deltas.Add (new Offsets (oldOff, offset, bs));
					offset += bs;
					break;
				default:
					throw new IOException ("Bad delta command:"
					+ command.ToString ("X"));
				}
			}
			throw new IOException ("Didn't recieve RS_OP_END.");
		}


		/// <summary>
		/// Patch the file at <code>filePath</code> using <code>deltas</code>, writing the
		/// patched file to <code>output stream</code>.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <param name="deltas">Deltas.</param>
		/// <param name="stream">Stream.</param>
		public void rebuildFile (String filePath, List<Delta> deltas, Stream stream)
		{
			using (FileStream fs = new FileStream (filePath, FileMode.Open)) {
				byte[] buf = new byte[config.blockLength];

				foreach (Delta delta in deltas) {
					if (delta is DataBlock) {
						DataBlock block = delta as DataBlock;
						stream.Write (block.getData (), 0, block.getBlockLength ());
					} else {
						Offsets offset = delta as Offsets;
						fs.Seek (offset.getOldOffset (), SeekOrigin.Begin);

						int len = 0, total = 0;
						do {
							len = fs.Read (buf, 0, buf.Length);
							total += len;
							stream.Write (buf, 0, len);
						} while (total < delta.getBlockLength ());
					}
				}
			}
		}

		/// <summary>
		/// Write a "COPY" command to output stream
		/// </summary>
		/// <param name="off">Off.</param>
		/// <param name="stream">output Stream.</param>
		private static void writeCopy (Offsets off, Stream stream)
		{
			stream.WriteByte (OP_COPY_N4_N4);
			writeInt (off.getOldOffset (), 4, stream);
			writeInt (off.getBlockLength (), stream);
		}

		/// <summary>
		/// Write a "LITERAL" command to output stream
		/// </summary>
		/// <param name="d">D.</param>
		/// <param name="stream">Stream.</param>
		private static void writeLiteral (DataBlock d, Stream stream)
		{
			byte cmd = 0;
			int param_len;

			switch (param_len = integerLength (d.getBlockLength ())) {
			case 1:
				cmd = OP_LITERAL_N1;
				break;
			case 2:
				cmd = OP_LITERAL_N2;
				break;
			case 4:
				cmd = OP_LITERAL_N4;
				break;
			}

			stream.WriteByte (cmd);
			writeInt (d.getBlockLength (), param_len, stream);
			stream.Write (d.getData (), 0, d.getBlockLength ());
		}

		/// <summary>
		/// Write a four-byte integer in big-endian byte order to <code>fs</code>.
		/// </summary>
		/// <param name="i">The index.</param>
		/// <param name="fs">Fs.</param>
		private static void writeInt (int i, Stream stream)
		{
			writeInt (i, 4, stream);
		}

		/// <summary>
		/// Write the lowest <code>len</code> bytes of <code>l</code> to
		/// output stream in big-endian byte order.
		/// </summary>
		/// <param name="l">L.</param>
		/// <param name="len">Length.</param>
		/// <param name="stream">Stream.</param>
		private static void writeInt (long l, int len, Stream stream)
		{
			for (int i = len - 1; i >= 0; i--) {
				stream.WriteByte ((byte)((l >> i * 8) & 0x000000ff));
			}
		}

		/// <summary>
		/// Read a four-byte big-endian integer from the input stream.
		/// </summary>
		/// <returns>The int.</returns>
		/// <param name="stream">Stream.</param>
		private static int readInt (Stream stream)
		{
			int i = 0;
			for (int j = 3; j >= 0; j--) {
				int k = stream.ReadByte ();
				if (k == -1)
					throw new EndOfStreamException ();
				i |= (k & 0xff) << 8 * j;
			}
			return i;
		}

		/// <summary>
		/// Read a variable-length integer from the input stream. This method reads
		/// <code>len</code> bytes from <code>in</code>, interpolating them as
		/// composing a big-endian integer.
		/// </summary>
		/// <returns>The int.</returns>
		/// <param name="len">Length.</param>
		/// <param name="stream">Stream.</param>
		private static long readInt (int len, Stream stream)
		{
			long i = 0;
			for (int j = len - 1; j >= 0; j--) {
				int k = stream.ReadByte ();
				if (k == -1)
					throw new EndOfStreamException ();
				i |= (k & 0xff) << 8 * j;
			}
			return i;
		}

		/// <summary>
		/// Check if a long integer needs to be represented by 1, 2, 4 or 8 bytes.
		/// </summary>
		/// <returns>The length.</returns>
		/// <param name="l">L.</param>
		private static int integerLength (long l)
		{
			if ((l & ~0xffL) == 0) {
				return 1;
			} else if ((l & ~0xffffL) == 0) {
				return 2;
			} else if ((l & ~0xffffffffL) == 0) {
				return 4;
			}
			return 8;
		}

	}
}

