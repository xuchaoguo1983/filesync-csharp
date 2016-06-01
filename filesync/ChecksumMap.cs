using System;
using System.Collections.Generic;

namespace filesync
{
	public class ChecksumMap
	{
		private Dictionary<int, List<ChecksumPair>> dataMap = new Dictionary<int, List<ChecksumPair>> ();

		public ChecksumMap ()
		{
		}

		public void Reset (List<ChecksumPair> pairs)
		{
			dataMap.Clear ();

			if (pairs != null && pairs.Count > 0) {
				foreach (ChecksumPair pair in pairs)
					this.Add (pair);
			}
		}

		public void Add (ChecksumPair pair)
		{
			List<ChecksumPair> list;
			if (!dataMap.TryGetValue(pair.Weak, out list)) {
				list = new List<ChecksumPair> ();
				dataMap.Add (pair.Weak, list);
			}

			list.Add (pair);
		}

		public bool IsExist (int week)
		{
			return dataMap.ContainsKey (week);
		}

		public ChecksumPair GetByStrong (int week, StrongKey strongKey)
		{
			List<ChecksumPair> pairs = dataMap [week];
			if (pairs == null || pairs.Count == 0)
				return null;

			foreach (ChecksumPair pair in pairs) {
				if (pair.Strong.Equals (strongKey))
					return pair;
			}

			return null;
		}
	}
}

