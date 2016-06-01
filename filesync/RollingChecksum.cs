using System;

namespace filesync
{
	public interface RollingChecksum
	{
		/// <summary>
		/// Returns the currently-computed 32-bit checksum.
		/// </summary>
		/// <returns>The value.</returns>
		Int32 getValue();

		/// <summary>
		/// Resets the internal state of the checksum, so it may be re-used later.
		/// </summary>
		void reset();

		/// <summary>
		/// Update the checksum with a single byte. This is where the "rolling"
		/// method is used.
		/// </summary>
		/// <param name="bt">Bt.</param>
		void roll(byte bt);

		/// <summary>
		/// Update the checksum by simply "trimming" the least-recently-updated byte
		/// from the internal state. Most, but not all, checksums can support this.
		/// </summary>
		void trim();

		/// <summary>
		/// Replaces the current internal state with entirely new data.
		/// </summary>
		/// <param name="buf">Buffer.</param>
		/// <param name="offset">Offset.</param>
		/// <param name="length">Length.</param>
		void check(byte[] buf, int offset, int length);

	}
}

