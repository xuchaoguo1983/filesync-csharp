using System;

namespace filesync
{
	/// <summary>
	/// This class represents an update to a file or array of bytes wherein the bytes
	/// themselves have not changed, but have moved to another location. This is
	/// represented by three fields: the offset in the original data, the offset in
	/// the new data, and the length, in bytes, of this block.
	/// </summary>
	public class Offsets : Delta
	{
		private long oldOffset;

		private long newOffset;

		private int blockLength;

		public Offsets (long oldOffset, long newOffset, int blockLength)
		{
			this.oldOffset = oldOffset;
			this.newOffset = newOffset;
			this.blockLength = blockLength;
		}

		public long getOldOffset ()
		{
			return oldOffset;
		}

		public long getWriteOffset ()
		{
			return newOffset;
		}

		public int getBlockLength ()
		{
			return blockLength;
		}


	}
}

