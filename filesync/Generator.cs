using System;
using System.Collections.Generic;
using System.IO;

namespace filesync
{
	/// <summary>
	/// Checksum generation methods.
	/// </summary>
	public class Generator
	{
		private Configuration config;

		public Generator (Configuration config)
		{
			this.config = config;
		}

		/// <summary>
		/// Generate checksums over a portion of abyte array, with a specified base
		/// offset. This <code>baseOffset</code> is added to the offset stored in
		/// each {@link ChecksumPair}.
		/// </summary>
		/// <returns>The sums.</returns>
		/// <param name="buf">Buffer.</param>
		/// <param name="off">Off.</param>
		/// <param name="len">Length.</param>
		/// <param name="baseOffset">Base offset.</param>
		public List<ChecksumPair> generateSums (byte[] buf, int off, int len,
		                                        long baseOffset)
		{
			int count = (len + (config.blockLength - 1)) / config.blockLength;
			int offset = off;
			List<ChecksumPair> sums = new List<ChecksumPair> ();

			for (int i = 0; i < count; i++) {
				int n = Math.Min (len, config.blockLength);
				ChecksumPair pair = generateSum (buf, offset, n, offset + baseOffset);
				pair.Seq = i;

				sums.Add (pair);
				len -= n;
				offset += n;
			}

			return sums;
		}

		/// <summary>
		/// Generate checksums for an entire file.
		/// </summary>
		/// <returns>The sums.</returns>
		/// <param name="filePath">File path.</param>
		public List<ChecksumPair> generateSums (String filePath)
		{
			using (FileStream fs = new FileStream (filePath, FileMode.Open)) {
				return generateSums (fs);
			}
		}

		/// <summary>
		/// Generate checksums for a input stream
		/// </summary>
		/// <returns>The sums.</returns>
		/// <param name="stream">Stream.</param>
		public List<ChecksumPair> generateSums (Stream stream)
		{
			List<ChecksumPair> sums = new List<ChecksumPair> ();

			byte[] buf = new byte[config.blockLength];
			long offset = 0;
			int len = 0;
			int seq = 0;

			while ((len = stream.Read (buf, 0, buf.Length)) > 0) {
				ChecksumPair pair = generateSum (buf, 0, len, offset);
				pair.Seq = seq;
				sums.Add (pair);
				offset += len;
				seq++;
			}

			return sums;
		}

		/// <summary>
		/// Generate a sum pair for a portion of a byte array.
		/// </summary>
		/// <returns>The sum.</returns>
		/// <param name="buf">Buffer.</param>
		/// <param name="off">Off.</param>
		/// <param name="len">Length.</param>
		/// <param name="fileOffset">File offset.</param>
		private ChecksumPair generateSum (byte[] buf, int off, int len,
		                                  long fileOffset)
		{
			ChecksumPair p = new ChecksumPair ();
			config.weakSum.check (buf, off, len);
			config.strongSum.engineUpdate (buf, off, len);
			if (config.checksumSeed != null) {
				config.strongSum.engineUpdate (config.checksumSeed, 0, config.checksumSeed.Length);
			}
			p.Weak = config.weakSum.getValue ();
			byte[] bytes = new byte[config.strongSumLength];
			Array.Copy (config.strongSum.engineDigest (), 0, bytes, 0, bytes.Length);

			p.Strong = new StrongKey (bytes);
			p.Offset = fileOffset;
			p.Length = len;
			return p;
		}
	}
}

