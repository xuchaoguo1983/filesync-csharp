using System;

namespace filesync
{
	/// <summary>
	/// This is the {@link Delta} in the rsync algorithm that introduces new data. It
	/// is an array of bytes and an offset, such that the updated file should contain
	/// this block at the given offset.
	/// </summary>
	public class DataBlock : Delta
	{
		private byte[] data;

		private long offset;

		public DataBlock (long offset, byte[] data)
		{
			this.offset = offset;
			this.data = (byte[])data.Clone ();
		}

		public DataBlock (long offset, byte[] data, int off, int len)
		{
			this.offset = offset;
			if (data.Length == len && off == 0) {
				this.data = (byte[])data.Clone ();
			} else {
				this.data = new byte[len];
				Array.Copy (data, off, this.data, 0, len);
			}
		}

		public int getBlockLength ()
		{
			return data.Length;
		}

		public long getWriteOffset ()
		{
			return offset;
		}

		public byte[] getData ()
		{
			return data;
		}
	}
}

