using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace ArchiveLoader
{   
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {            
            const int StartingPoint = 9800;
            string site = "ru.meta.stackoverflow.com";
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");
            string deleted_dir = Path.Combine(datadir, "deleted\\");
            int i1 = StartingPoint;
            int i2 = StartingPoint+99;
            Dictionary<int,object> posts;
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);
            if (!Directory.Exists(deleted_dir)) Directory.CreateDirectory(deleted_dir);

            SeApiClient client = new SeApiClient("https://api.stackexchange.com/2.2/",site );

            while (true)
            {
                Console.WriteLine("Loading posts {0} to {1}...",i1,i2);
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

            Console.WriteLine("Done");
            if (!Console.IsInputRedirected) Console.ReadKey();

        }
    }
}
