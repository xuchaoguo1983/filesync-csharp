using System;
using System.Security.Cryptography;

namespace filesync
{
	/// <summary>
	/// A Configuration is a mere collection of objects and values that compose a
	/// particular configuration for the algorithm, for example the message digest
	/// that computes the strong checksum.
	/// </summary>
	public class Configuration
	{
		/// <summary>
		/// The default block size.
		/// </summary>
		public const int BLOCK_LENGTH = 1024;

		/// <summary>
		/// The default strong sum length
		/// </summary>
		public const int STRONG_LENGTH = 8;

		/// <summary>
		/// The default chunk size.
		/// </summary>
		public const int CHUNK_SIZE = 32768;

		public const short CHAR_OFFSET = 31;

		/// <summary>
		/// The message digest that computes the stronger checksum.
		/// </summary>
		public MD4 strongSum;

		/// <summary>
		/// The rolling checksum.
		/// </summary>
		public RollingChecksum weakSum;

		/// <summary>
		/// The length of blocks to checksum.
		/// </summary>
		public int blockLength;

		/// <summary>
		/// The effective length of the strong sum.
		/// </summary>
		public int strongSumLength;

		/// <summary>
		/// The seed for the checksum, to perturb the strong checksum and help avoid
		/// collisions in plain rsync (or in similar applicaitons).
		/// </summary>
		public byte[] checksumSeed;

		/// <summary>
		/// The maximum size of byte arrays to create, when they are needed. This
		///  vale defaults to 32 kilobytes.
		/// </summary>
		public int chunkSize;

		public Configuration() {
			blockLength = BLOCK_LENGTH;
			strongSumLength = STRONG_LENGTH;
			chunkSize = CHUNK_SIZE;
			strongSum = new MD4 ();
			weakSum = new Adler32RollingChecksum (CHAR_OFFSET);
		}
	}
}

