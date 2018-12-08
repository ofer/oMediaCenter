using oMediaCenter.MetaDatabase;
using System;
using System.IO;

namespace oMediaCenter.ImdbImporter
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Imdb TSV importer");
			MetaDataContext metaDataContext = new MetaDataContext();

			int numEntriesProcessed = 0;

			using (FileStream stream = File.OpenRead(@"C:\Users\Ofer Achler\Downloads\title.basics.tsv\data.tsv"))
			{
				using (StreamReader tr = new StreamReader(stream))
				{
					while (!tr.EndOfStream)
					{
						string line = tr.ReadLine();
						string[] splitLine = line.Split('	');

						switch (splitLine[1])
						{
							case "movie":
							case "short":
							case "tvSeries":
								metaDataContext.MediaDatum.Add(new MediaData() { OriginalString = line, Title = splitLine[2], LowercaseTitle = splitLine[2].ToSearchableString() });
								numEntriesProcessed++;
								break;
						}

						if (numEntriesProcessed % 100000 == 0)
						{
							metaDataContext.SaveChanges();
							Console.WriteLine($"Saved {numEntriesProcessed} to db so far.");
						}

					}

					metaDataContext.SaveChanges();
				}
			}

			Console.WriteLine($"Saved {numEntriesProcessed} to db.");

			Console.ReadKey();
		}
	}
}
