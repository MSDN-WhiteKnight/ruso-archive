using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using RuSoLib;
using Integration.Git;

namespace ArchiveLoader
{
    class Program
    {
        static void MoveFile(string src, string dst, bool overwrite)
        {
            bool copied;
            Exception copy_error = null;

            try
            {
                File.Copy(src, dst, overwrite);
                copied = true;
            }
            catch (Exception ex)
            {
                copied = false;
                copy_error = ex;
            }

            if (copied) File.Delete(src);
            else throw new IOException("Failed to move file " + src, copy_error);
        }

        static void LoadDataMarkdown()
        {
            const int StartingPoint = 11000;
            string site = "ru.meta.stackoverflow.com";
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts-raw\\");
            string postsdir2 = Path.Combine(datadir, "posts\\");
            string deleted_dir = Path.Combine(datadir, "deleted\\");
            int i1 = StartingPoint;
            int i2 = StartingPoint + 99;
            Dictionary<int, object> posts;
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);
            if (!Directory.Exists(deleted_dir)) Directory.CreateDirectory(deleted_dir);
            if (!Directory.Exists(postsdir2)) Directory.CreateDirectory(postsdir2);

            SeApiClient client = new SeApiClient(Archive.APIURL, site);

            Console.WriteLine(" Updating archive data: {0}", DateTime.Now);

            while (true)
            {
                Console.WriteLine("Loading posts {0} to {1}...", i1, i2);
                posts = client.LoadPostsRange(i1, i2);

                if (posts.Count == 0) break;

                Console.WriteLine("{0} posts loaded", posts.Count);

                for (int i = i1; i <= i2; i++)
                {
                    path = Path.Combine(postsdir, i.ToString() + ".md");

                    if (!posts.ContainsKey(i))
                    {
                        if (File.Exists(path))
                        {
                            Console.WriteLine("Found deleted post: {0}", i);
                            string path2 = Path.Combine(postsdir2, "Q" + i.ToString() + ".md");
                            string newpath;                            

                            if (File.Exists(path2))
                            {
                                newpath = Path.Combine(deleted_dir, "Q" + i.ToString() + ".md");
                                MoveFile(path2, newpath, true);
                            }

                            path2 = Path.Combine(postsdir2, "A" + i.ToString() + ".md");
                            if (File.Exists(path2))
                            {
                                newpath = Path.Combine(deleted_dir, "A" + i.ToString() + ".md");
                                MoveFile(path2, newpath, true);
                            }
                            File.Delete(path);
                        }
                    }
                    else
                    {
                        using (TextWriter wr = new StreamWriter(path, false))
                        {
                            PostMarkdown post = PostMarkdown.FromJsonData(site, posts[i]);
                            post.ToMarkdown(wr);
                        }
                    }
                }

                i1 = i2 + 1;
                i2 = i1 + 99;
            }

            //Scan posts and split to questions and answers
            List<int> question_ids = new List<int>();
            List<int> answer_ids = new List<int>();
            string[] files = Directory.GetFiles(postsdir, "*.md");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                string idstr = file;
                int id;

                if (!Int32.TryParse(idstr, out id))
                {
                    Console.WriteLine("Bad post id = {0} in file {1}", idstr, files[i]);
                    continue;
                }

                try
                {
                    PostMarkdown post=null;
                    using (TextReader read = new StreamReader(files[i],Encoding.UTF8))
                    {
                        post = PostMarkdown.FromMarkdown(site, read);
                    }

                    if (post.PostType == "question") question_ids.Add(id);
                    else if (post.PostType == "answer") answer_ids.Add(id);
                    else Console.WriteLine("Unknown post type: {0} in {1}", post.PostType, files[i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file " + files[i]);
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }

            int[] sequence;

            //load questions
            int[] q_arr = question_ids.ToArray();
            i1 = 0;
            i2 = 99;
            if (i2 >= q_arr.Length) i2 = q_arr.Length - 1;

            while (true)
            {
                sequence = new int[i2 - i1 + 1];
                Console.WriteLine("Loading questions from #{0} to #{1}...", i1, i2);
                Array.Copy(q_arr, i1, sequence, 0, sequence.Length);
                Dictionary<int, object> questions = client.LoadQuestionsSequence(sequence);

                Console.WriteLine("{0} questions loaded", questions.Count);                

                for (int i = 0; i < sequence.Length; i++)
                {
                    int id = sequence[i];
                    path = Path.Combine(postsdir2, "Q" + id.ToString() + ".md");

                    if (!questions.ContainsKey(id))
                    {
                        if (File.Exists(path))
                        {
                            Console.WriteLine("Found deleted question: {0}", id);
                            File.Move(
                                Path.Combine(postsdir2, "Q" + id.ToString() + ".md"),
                                Path.Combine(deleted_dir, "Q" + id.ToString() + ".md")
                                );
                        }
                    }
                    else
                    {
                        using (TextWriter wr = new StreamWriter(path, false,Encoding.UTF8))
                        {
                            QuestionMarkdown q = QuestionMarkdown.FromJsonData(site, questions[id]);
                            q.ToMarkdown(wr);
                        }
                    }
                }

                i1 = i2 + 1;
                if (i1 >= q_arr.Length) break;
                i2 = i1 + 99;
                if (i2 >= q_arr.Length) i2 = q_arr.Length - 1;
            }

            //load answers
            int[] a_arr = answer_ids.ToArray();
            i1 = 0;
            i2 = 99;
            if (i2 >= a_arr.Length) i2 = a_arr.Length - 1;

            while (true)
            {
                sequence = new int[i2 - i1 + 1];
                Console.WriteLine("Loading answers from #{0} to #{1}...", i1, i2);
                Array.Copy(a_arr, i1, sequence, 0, sequence.Length);
                Dictionary<int, object> answers = client.LoadAnswersSequence(sequence);

                Console.WriteLine("{0} answers loaded", answers.Count);                

                for (int i = 0; i < sequence.Length; i++)
                {
                    int id = sequence[i];
                    path = Path.Combine(postsdir2, "A" + id.ToString() + ".md");

                    if (!answers.ContainsKey(id))
                    {
                        if (File.Exists(path))
                        {
                            Console.WriteLine("Found deleted answer: {0}", id);
                            File.Move(
                                Path.Combine(postsdir2, "A" + id.ToString() + ".md"),
                                Path.Combine(deleted_dir, "A" + id.ToString() + ".md")
                                );
                        }
                    }
                    else
                    {
                        using (TextWriter wr = new StreamWriter(path, false,Encoding.UTF8))
                        {
                            AnswerMarkdown a = AnswerMarkdown.FromJsonData(site,answers[id]);
                            a.ToMarkdown(wr);
                        }
                    }
                }

                i1 = i2 + 1;
                if (i1 >= a_arr.Length) break;
                i2 = i1 + 99;
                if (i2 >= a_arr.Length) i2 = a_arr.Length - 1;
            }
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [STAThread]
        static void Main(string[] args)
        {
            HTML.SiteURL = "https://github.com/MSDN-WhiteKnight/ruso-archive/";
            HTML.SiteTitle = "RuSO Archive";
            HTML.LicenseURL = "https://github.com/MSDN-WhiteKnight/ruso-archive/blob/master/LICENSE";
            HTML.EnableAttribution = true;

            if (args.Length == 0)
            {
                LoadDataMarkdown();

                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
            else if (args.Length >= 3 && args[0] == "saveq")
            {
                Archive.SaveQuestion(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "savea")
            {
                Archive.SaveSingleAnswer(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "saveu")
            {
                Archive.SaveUserAnswers(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "sync")
            {
                Archive.SaveQuestionsForSavedAnswers(args[1], args[2]);
                Console.WriteLine("Done");
            }
            else if (args.Length >= 1 && args[0] == "generate")
            {
                Archive.Generate("ru.meta.stackoverflow.com", "posts", "Posts");
                Archive.Generate("ru.meta.stackoverflow.com", "deleted", "Deleted posts");
                Archive.Generate("ru.stackoverflow.com", "posts", "Posts");
				Archive.Generate("ru.stackoverflow.com", "deleted", "Deleted posts");
                Console.WriteLine("Done");
            }
            else if (args.Length >= 1 && args[0] == "load")
            {
                StreamWriter wr = new StreamWriter("ArchiveLoader.log", true);
                using (wr)
                {
                    try
                    {
                        wr.AutoFlush = true;
                        Console.SetOut(wr);
                        Console.SetError(wr);
                        Console.WriteLine();
                        Console.WriteLine(" Pulling latest changes from remote repository: {0}", DateTime.Now);
                        Console.WriteLine(GitBash.ExecuteCommand("cd ../../../../../; git pull"));
                        LoadDataMarkdown();
                        Console.WriteLine("Done");
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }
                    finally
                    {
                        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                        standardOutput.AutoFlush = true;
                        Console.SetOut(standardOutput);

                        var standardError = new StreamWriter(Console.OpenStandardError());
                        standardError.AutoFlush = true;
                        Console.SetError(standardError);
                    }
                }
            }
            else if (args.Length >= 1 && args[0] == "publish")
            {
                IntPtr hwnd = GetConsoleWindow();
                if (hwnd != null) ShowWindow(hwnd, SW_HIDE);

                StreamWriter wr=null;
                int c = 0;

                while (true)
                {
                    try
                    {
                        wr = new StreamWriter("ArchiveLoader.log", true);
                    }
                    catch (IOException iex) 
                    {
                        if (c > 20)
                        {
                            Exception ex = new Exception(c.ToString() + " attempts to open ArchiveLoader.log failed.", iex);
                            throw ex;
                        }                            
                    }

                    if (wr != null) break;

                    c++;
                    Thread.Sleep(10000);
                }

                using (wr)
                {
                    try
                    {
                        if (wr != null)
                        {
                            wr.AutoFlush = true;
                            Console.SetOut(wr);
                            Console.SetError(wr);
                        }

                        Console.WriteLine();
                        Console.WriteLine(" Generating website: {0}", DateTime.Now);

                        if (c > 0) Console.WriteLine("Warning: ArchiveLoader.log opened with " + c.ToString() + " attempts!");

                        Archive.Generate("ru.meta.stackoverflow.com", "posts", "Posts");
                        Archive.Generate("ru.meta.stackoverflow.com", "deleted", "Deleted posts");
                        Archive.Generate("ru.stackoverflow.com", "posts", "Posts");
                        Archive.Generate("ru.stackoverflow.com", "deleted", "Deleted posts");

                        Console.WriteLine();
                        Console.WriteLine(" Publishing pending changes to remote repository: {0}", DateTime.Now);
                        
                        string command = "cd ../../../../../; " +
                            "$DOCFX_PATH/docfx.exe docfx.json; " +
                            "git add tools/data/\\*.md &>/dev/null; " +
                            "git add html/ &>/dev/null; ";

                        Console.WriteLine(GitBash.ExecuteCommand(command));

                        command = "cd ../../../../../; " +
                            "git commit -m 'Update website (auto)'; " +
                            "git push origin master";

                        Console.WriteLine(GitBash.ExecuteCommand(command));

                        Console.WriteLine("Done");
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }
                    finally
                    {
                        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                        standardOutput.AutoFlush = true;
                        Console.SetOut(standardOutput);

                        var standardError = new StreamWriter(Console.OpenStandardError());
                        standardError.AutoFlush = true;
                        Console.SetError(standardError);
                    }
                }
            }
            else
            {
                Console.WriteLine(" *** RuSO Archive Tool *** ");
                Console.WriteLine(" Usage: ");
                Console.WriteLine("ArchiveLoader saveq [site] [question_id] | Save question and its answers");
                Console.WriteLine("ArchiveLoader savea [site] [question_id] | Save single answer");
                Console.WriteLine("ArchiveLoader saveu [site] [user_id]     | Save all answers of user");
                Console.WriteLine("ArchiveLoader generate                   | Generate website");
                Console.WriteLine();

                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
        }
    }
}
