using System;

namespace filesync
{
	public class ChecksumPair
	{
		private long offset;
		private int length;
		private int weak;
		private StrongKey strong;
		private int seq;

		public long Offset {
			get {
				return this.offset;
			}
			set {
				offset = value;
			}
		}

		public int Length {
			get {
				return this.length;
			}
			set {
				length = value;
			}
		}

		public int Weak {
			get {
				return this.weak;
			}
			set {
				weak = value;
			}
		}

		public StrongKey Strong {
			get {
				return this.strong;
			}
			set {
				strong = value;
			}
		}

		public int Seq {
			get {
				return this.seq;
			}
			set {
				seq = value;
			}
		}

		public ChecksumPair ()
		{
			
		}

		public ChecksumPair (long offset, int length, int weak, byte[] strong, int seq)
		{
			this.offset = offset;
			this.length = length;
			this.weak = weak;
			this.strong = new StrongKey(strong);
			this.seq = seq;
		}

		public ChecksumPair(long offset, int weak, byte[] strong) {
			this.offset = offset;
			this.weak = weak;
			this.strong = new StrongKey(strong);
			this.length = 0;
			this.seq = 0;
		}

		public override string ToString ()
		{
			return "len=" + length + " offset=" + offset + " weak=" + weak
				+ " strong=" + BitConverter.ToString(strong.Bytes).Replace("-","");
		}
	}

	public class StrongKey
	{
		public byte[] bytes;

		public byte[] Bytes {
			get {
				return this.bytes;
			}
		}

		public StrongKey (byte[] bytes)
		{
			this.bytes = bytes;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(StrongKey))
				return false;
			StrongKey other = (StrongKey)obj;

			if (bytes.Length != other.bytes.Length)
				return false;

			int index = 0;
			int length = this.bytes.Length;
			while (index < length) {
				if (this.bytes [index] != other.bytes [index]) {
					return false;
				}
				index++;
			}

			return true;
		}


		public override int GetHashCode ()
		{
			unchecked {
				return (bytes != null ? bytes.GetHashCode () : 0);
			}
		}
		
	}
}

