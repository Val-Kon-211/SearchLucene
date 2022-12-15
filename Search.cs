using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;
using System.IO;
using System.Linq;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Analysis.En;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spell;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SearchPro
{
    class Search
    {
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        static IndexWriter writer;
        static Analyzer analyzer;

        static void CreateIndex(LuceneDirectory indexDir, string PathToFolder)
        {
            //Create an analyzer to process the text 
            analyzer = new EnglishAnalyzer(luceneVersion);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
            indexConfig.OpenMode = OpenMode.CREATE;
            writer = new IndexWriter(indexDir, indexConfig);

            //Add documents to the index

            foreach (string filename in System.IO.Directory.EnumerateFiles(PathToFolder, "*.txt"))
            {
                Document doc = new Document();

                int i1 = filename.IndexOf("\\");
                int i2 = filename.IndexOf(".txt");
                string title = filename.Substring(i1 + 1, i2 - i1 - 1);
                string[] read_text = File.ReadAllLines(filename);
                string text = "";
                
                if (read_text.Length != 0)
                {
                    text = read_text[0];
                }
                else { continue; }

                doc.Add(new TextField("title", title, Field.Store.YES));
                doc.Add(new TextField("text", text, Field.Store.YES));
                writer.AddDocument(doc);
            }

            //Flush and commit the index data to the directory
            writer.Commit();
        }

        static string[] GetSuggestions(string searchText, DirectoryReader reader)
        {
            Lucene.Net.Search.Spell.SpellChecker spellChecker = new Lucene.Net.Search.Spell.SpellChecker(new RAMDirectory());
            IndexWriterConfig config = new IndexWriterConfig(luceneVersion, analyzer);
            spellChecker.IndexDictionary(new LuceneDictionary(reader, "text"), config, fullMerge: false);

            string[] suggestions = spellChecker.SuggestSimilar(searchText, 4);
            return suggestions;
        }

        static void Search_fun(string searchText, string[] searchFields, int maxSearchItems)
        {
            searchText = searchText.ToLower();
            using (DirectoryReader reader = writer.GetReader(applyAllDeletes: true))
            {
                string[] suggestions = GetSuggestions(searchText, reader);

                IndexSearcher searcher = new IndexSearcher(reader);
                QueryParser parser = new MultiFieldQueryParser(luceneVersion, searchFields, analyzer);

                var searchTerms = suggestions;
                Console.WriteLine("Searching: " + searchText);
                Console.WriteLine("Searching: " + string.Join(", ", searchTerms));

                IEnumerable<ScoreDoc> foundDocs = new ScoreDoc[] { };

                foreach (var searchTerm in searchTerms)
                {
                    Query query = parser.Parse(searchTerm);
                    TopDocs topDocs = searcher.Search(query, n: maxSearchItems);         //indicate we want the first 3 results
                    Console.WriteLine($"Matching results: {topDocs.TotalHits}");
                    IEnumerable<ScoreDoc> tempDocs = topDocs.ScoreDocs.Take(maxSearchItems);
                    foundDocs = tempDocs.Union(foundDocs);
                }

                IEnumerable<ScoreDoc> resText = new ScoreDoc[] { };
                Query queryText = parser.Parse(searchText);
                TopDocs topDocs1 = searcher.Search(queryText, n: maxSearchItems);         //indicate we want the first 3 results
                Console.WriteLine($"Matching results: {topDocs1.TotalHits}");
                resText = topDocs1.ScoreDocs.Take(maxSearchItems).Union(resText);

                foundDocs = foundDocs.OrderByDescending(x => x.Score).Take(maxSearchItems).ToArray();
                foundDocs = resText.Union(foundDocs);

                int i = 0;
                Console.WriteLine();
                foreach (var doc in foundDocs)
                {
                    i++;
                    Document resultDoc = searcher.Doc(doc.Doc);
                    string titleR = resultDoc.Get("title");
                    string textR = resultDoc.Get("text");
                    Console.WriteLine($"Title of result {i}: {titleR}");
                    Console.WriteLine();
                }
            }
        }

        static void CreateAndSearchRu(string toSearch)
        {
            //Open the Directory using a Lucene Directory class
            string indexName = "game_index";
            string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

            using (LuceneDirectory indexDir = FSDirectory.Open(indexPath))
            {
                CreateIndex(indexDir, "C://Users//konva//Documents//STUDY//Searching//Files");

                Search_fun(toSearch, new string[] { "title", "text" }, maxSearchItems: 4867);
            }
        }

        static void Main(string[] args)
        {
            CreateAndSearchRu("Zelda Breath of the Wild 2");
            Console.ReadKey();
        }
    }
}
