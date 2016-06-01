using System;
using System.Collections.Generic;
using System.IO;

namespace filesync
{
	/// <summary>
	/// Methods for performing the checksum search. The result of a search is a
	/// list of {@link Delta} objects that, when applied to a
	/// method in {@link Rdiff}, will reconstruct the new version of the data.
	/// </summary>
	public class Matcher
	{
		private Configuration config;

		public Matcher (Configuration config)
		{
			this.config = config;
		}

		/// <summary>
		/// Search a portion of a byte buffer.
		/// </summary>
		/// <returns>The search.</returns>
		/// <param name="map">Map.</param>
		/// <param name="buf">Buffer.</param>
		/// <param name="offset">Offset.</param>
		/// <param name="len">Length.</param>
		private List<Delta> hashSearch (ChecksumMap map, byte[] buf, int offset,
		                                int len)
		{
			List<Delta> deltas = new List<Delta> ();

			int blockSize = Math.Min (config.blockLength, len);
			byte[] block = new byte[blockSize];
			byte[] bytes = new byte[config.strongSumLength];

			int i = offset;
			int rest = len;
			int idx = 0;
			int j = 0;
			do {
				blockSize = Math.Min (config.blockLength, rest);
				Array.Copy (buf, i, block, 0, blockSize);
				rest -= blockSize;

				config.weakSum.check (block, 0, blockSize);

				for (j = 0; j <= rest; j++) {
					if (j > 0) {
						config.weakSum.roll (buf [i + blockSize + j - 1]);
					}

					int weak = config.weakSum.getValue ();

					if (map.IsExist (weak)) {
						config.strongSum.engineUpdate (buf, i + j, blockSize);
						if (config.checksumSeed != null) {
							config.strongSum.engineUpdate (config.checksumSeed, 0, config.checksumSeed.Length);
						}

						Array.Copy (config.strongSum.engineDigest (), 0, bytes, 0,
							bytes.Length);

						ChecksumPair pair = map.GetByStrong (weak, new StrongKey (
							                    bytes));
						if (pair != null) {
							// matched
							if (j > 0) {
								DataBlock d = new DataBlock (idx, buf, i, j);
								deltas.Add (d);
								idx += j;
							}

							Offsets o = new Offsets (pair.Offset, idx,
								            blockSize);
							deltas.Add (o);

							idx += blockSize;
							i += (blockSize + j);
							break;
						}
					}
				}

				if (j > rest) {
					// no match
					DataBlock d = new DataBlock (idx, buf, i, rest + blockSize);
					deltas.Add (d);
					break;
				} else {
					rest -= j;
				}
			} while (rest > 0);

			return deltas;
		}


		public List<Delta> hashSearch (List<ChecksumPair> sums, string filePath)
		{
			using (FileStream fs = new FileStream (filePath, FileMode.Open)) {
				return hashSearch (sums, fs);
			}
		}

		/// <summary>
		/// Search an input stream and find the different deltas.
		/// </summary>
		/// <returns>The search.</returns>
		/// <param name="sums">Sums.</param>
		/// <param name="stream">Stream.</param>
		public List<Delta> hashSearch (List<ChecksumPair> sums, Stream stream)
		{
			List<Delta> deltas = new List<Delta> ();

			byte[] buffer = new byte[config.chunkSize];
			int len = 0;

			ChecksumMap map = new ChecksumMap();
			map.Reset(sums);

			while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) {
				List<Delta> list = this.hashSearch(map, buffer, 0, len);

				deltas.AddRange(list);
			}

			return deltas;
		}

	}
}

