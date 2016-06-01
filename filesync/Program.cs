using System;
using System.Collections.Generic;
using System.IO;

namespace filesync
{
	public class Program
	{
		public static void Main(string[] args) {
			Configuration c = new Configuration ();

			Rdiff proxy = new Rdiff (c);


			List<ChecksumPair> pairs = proxy.makeSignatures ("/Users/xuchaoguo/temp/rsync/sig/client.doc");

			proxy.writeSignatures (pairs, "/Users/xuchaoguo/temp/rsync/sig/sig.txt");

			using (FileStream fs = new FileStream ("/Users/xuchaoguo/temp/rsync/sig/server.doc", FileMode.Open)) {
				List<Delta> deltas = proxy.makeDeltas (pairs,fs );

				using (FileStream dfs = new FileStream ("/Users/xuchaoguo/temp/rsync/sig/delta.txt", FileMode.OpenOrCreate)) {
					proxy.writeDeltas (deltas, dfs);
				}

				using (FileStream ffs = new FileStream("/Users/xuchaoguo/temp/rsync/sig/latest.doc", FileMode.OpenOrCreate)){
					proxy.rebuildFile ("/Users/xuchaoguo/temp/rsync/sig/client.doc", deltas, ffs);
				}
			}
		}
	}
}

