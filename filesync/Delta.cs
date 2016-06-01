using System;

namespace filesync
{
	/// <summary>
	/// A Delta is, in the Rsync algorithm, one of two things: (1) a block of bytes
	/// and an offset, or (2) a pair of offsets, one old and one new.
	/// </summary>
	public interface Delta
	{
		/// <summary>
		/// The size of the block of data this class represents.
		/// </summary>
		/// <returns>The block length.</returns>
		int getBlockLength();

		/// <summary>
		/// Get the offset at which this Delta should be written.
		/// </summary>
		/// <returns>The write offset.</returns>
		long getWriteOffset();
	}
}

