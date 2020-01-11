using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace ArchiveLoader
{   
    class Program
    {
        const string APIURL = "https://api.stackexchange.com/2.2/";

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

            SeApiClient client = new SeApiClient(APIURL, site);

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

            SeApiClient client = new SeApiClient(APIURL, site);
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
                path = Path.Combine(postsdir, "A" + key.ToString() + ".json");
                File.WriteAllText(path, answers[key], Encoding.UTF8);
            }
        }

        static void SaveSingleAnswer(string site, int id)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts-json\\");
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);
            Console.WriteLine("Saving single answer {0} from {1}...", id, site);

            SeApiClient client = new SeApiClient(APIURL, site);
            string a = client.LoadSingleAnswer(id);

            if (a == null)
            {
                throw new Exception("Failed to load answer " + id.ToString() + " from " + site);
            }

            path = Path.Combine(postsdir, "A" + id.ToString() + ".json");
            File.WriteAllText(path, a, Encoding.UTF8);

            Console.WriteLine("Success");
        }

        static void Generate(string site)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts-json\\");
            string htmldir = "..\\..\\..\\..\\html\\" + site + "\\";
            string path;
            TextWriter wr;

            PostSet posts = PostSet.LoadFromDir(postsdir, site);
            Dictionary<int, Question> questions = posts.Questions;
            Console.WriteLine("Questions: {0}", questions.Count);

            if (!Directory.Exists(htmldir)) Directory.CreateDirectory(htmldir);

            foreach (int q in questions.Keys)
            {
                string title = questions[q].DataDynamic.title;
                Console.WriteLine(title);

                path = Path.Combine(htmldir, q.ToString() + ".html");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    questions[q].GenerateHTML(wr);
                }
            }

            Console.WriteLine("Single answers: {0}", posts.SingleAnswers.Count);

            foreach (int a in posts.SingleAnswers.Keys)
            {                
                path = Path.Combine(htmldir, a.ToString() + ".html");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    HTML.RenderHeader(a,wr);
                    posts.SingleAnswers[a].GenerateHTML(wr);
                    HTML.RenderBottom(wr);
                }
            }

            Console.WriteLine("Generating TOC...");
            path = Path.Combine(htmldir, "index.html");
            wr = new StreamWriter(path, false);
            using (wr)
            {
                HTML.RenderTOC(site, posts, wr);
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
