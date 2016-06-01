using System;

namespace filesync
{
	/// <summary>
	///A simple 32-bit "rolling" checksum. This checksum algorithm is based upon the
	/// algorithm outlined in the paper "The rsync algorithm" by Andrew Tridgell and
	/// Paul Mackerras. The algorithm works in such a way that if one knows the sum
	/// of a block <em>X<sub>k</sub>...X<sub>l</sub></em>, then it is a simple matter
	/// to compute the sum for <em>X<sub>k+1</sub>...X<sub>l+1</sub></em>.
	/// </summary>
	public class Adler32RollingChecksum : RollingChecksum
	{
		// Constants and variables.
		// -----------------------------------------------------------------

		private int char_offset;
		private int a;
		private int b;
		private int k;
		private int l;
		private sbyte[] block;

		public Adler32RollingChecksum(int char_offset) {
			this.char_offset = char_offset;
			a = b = 0;
			k = 0;
		}

		public Adler32RollingChecksum() : this(0) {
		}
			
		public int getValue() {
			return (a & 0xffff) | (b << 16);
		}
			
		public void reset() {
			k = 0;
			a = b = 0;
			l = 0;
		}

		public void roll(byte bt) {
			sbyte sbt = (sbyte)bt;

			a -= block[k] + char_offset;
			b -= l * (block[k] + char_offset);
			a += sbt + char_offset;
			b += a;
			block[k] = sbt;
			k++;
			if (k == l)
				k = 0;
		}

		public void trim() {
			a -= block[k % block.Length] + char_offset;
			b -= l * (block[k % block.Length] + char_offset);
			k++;
			l--;
		}

		public void check(byte[] buf, int off, int len) {
			block = new sbyte[len];
			Buffer.BlockCopy(buf, off, block, 0, len);

			reset();
			l = block.Length;
			int i;

			for (i = 0; i < block.Length - 4; i += 4) {
				b += 4 * (a + block[i]) + 3 * block[i + 1] + 2 * block[i + 2]
					+ block[i + 3] + 10 * char_offset;
				a += block[i] + block[i + 1] + block[i + 2] + block[i + 3] + 4
					* char_offset;
			}
			for (; i < block.Length; i++) {
				a += block[i] + char_offset;
				b += a;
			}
		}
	}
}

