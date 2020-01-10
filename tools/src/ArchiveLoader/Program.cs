using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace ArchiveLoader
{   
    class Program
    {
        static void LoadData()
        {
            const int StartingPoint = 9800;
            string site = "ru.meta.stackoverflow.com";
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");
            string deleted_dir = Path.Combine(datadir, "deleted\\");
            int i1 = StartingPoint;
            int i2 = StartingPoint + 99;
            Dictionary<int, object> posts;
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);
            if (!Directory.Exists(deleted_dir)) Directory.CreateDirectory(deleted_dir);

            SeApiClient client = new SeApiClient("https://api.stackexchange.com/2.2/", site);

            while (true)
            {
                Console.WriteLine("Loading posts {0} to {1}...", i1, i2);
                posts = client.LoadPostsRange(i1, i2);

                if (posts.Count == 0) break;

                Console.WriteLine("{0} posts loaded", posts.Count);

                for (int i = i1; i <= i2; i++)
                {
                    path = Path.Combine(postsdir, i.ToString() + ".html");

                    if (!posts.ContainsKey(i))
                    {
                        if (File.Exists(path))
                        {
                            Console.WriteLine("Found deleted post: {0}", i);
                            File.Move(path, Path.Combine(deleted_dir, i.ToString() + ".html"));
                        }
                    }
                    else
                    {
                        using (TextWriter wr = new StreamWriter(path, false))
                        {
                            HTML.RenderPost(posts[i], wr);
                        }
                    }
                }

                i1 = i2 + 1;
                i2 = i1 + 99;
            }
        }

        static void SaveQuestion(string site,int id)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts-json\\");                        
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);            

            SeApiClient client = new SeApiClient("https://api.stackexchange.com/2.2/", site);
            string q = client.LoadQuestion(id);

            if (q == null)
            {
                throw new Exception("Failed to load question "+id.ToString()+" from "+site);
            }

            path = Path.Combine(postsdir, "Q"+id.ToString() + ".json");
            File.WriteAllText(path, q, Encoding.UTF8);

            Dictionary<int, string> answers = client.LoadQuestionAnswers(id);
            Console.WriteLine("Saving {0} answers...", answers.Count);

            foreach (int key in answers.Keys)
            {
                path = Path.Combine(postsdir, key.ToString() + ".json");
                File.WriteAllText(path, answers[key], Encoding.UTF8);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                LoadData();
            }
            else if (args.Length >= 3 && args[0] == "-saveq")
            {
                SaveQuestion(args[1], Convert.ToInt32(args[2]));
            }
            else
            {
                Console.WriteLine(" *** RuSO Archive Tool *** ");
                Console.WriteLine(" Usage: ");
                Console.WriteLine("ArchiveLoader -saveq [site] [question_id] | Save question and its answers");
                Console.WriteLine();
            }
            
            Console.WriteLine("Done");

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

        }
    }
}
