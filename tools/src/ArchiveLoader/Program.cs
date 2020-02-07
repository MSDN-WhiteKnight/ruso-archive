using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Integration.Git;

namespace ArchiveLoader
{   
    class Program
    {
        const string APIURL = "https://api.stackexchange.com/2.2/";

        static void LoadDataMarkdown()
        {
            const int StartingPoint = 9800;
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

            SeApiClient client = new SeApiClient(APIURL, site);

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
                            if (File.Exists(path2))
                            {
                                File.Move(path2, Path.Combine(deleted_dir, "Q" + i.ToString() + ".md"));
                            }

                            path2 = Path.Combine(postsdir2, "A" + i.ToString() + ".md");
                            if (File.Exists(path2))
                            {
                                File.Move(path2, Path.Combine(deleted_dir, "A" + i.ToString() + ".md"));
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

        static void SaveQuestion(string site,int id)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");                        
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);

            SeApiClient client = new SeApiClient(APIURL, site);
            string q = client.LoadQuestion(id);

            if (q == null)
            {
                throw new Exception("Failed to load question "+id.ToString()+" from "+site);
            }

            path = Path.Combine(postsdir, "Q"+id.ToString() + ".md");
            TextWriter wr = new StreamWriter(path, false, Encoding.UTF8);
            using (wr)
            {
                QuestionMarkdown post = QuestionMarkdown.FromJsonData(site, q);
                post.ToMarkdown(wr);
            }

            Dictionary<int, string> answers = client.LoadQuestionAnswers(id);
            Console.WriteLine("Saving {0} answers...", answers.Count);

            foreach (int key in answers.Keys)
            {
                path = Path.Combine(postsdir, "A" + key.ToString() + ".md");
                wr = new StreamWriter(path, false, Encoding.UTF8);
                using (wr)
                {
                    AnswerMarkdown post = AnswerMarkdown.FromJsonData(site, answers[key]);
                    post.ToMarkdown(wr);
                }
            }
        }

        static void SaveSingleAnswer(string site, int id)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);
            Console.WriteLine("Saving single answer {0} from {1}...", id, site);

            SeApiClient client = new SeApiClient(APIURL, site);
            string a = client.LoadSingleAnswer(id);

            if (a == null)
            {
                throw new Exception("Failed to load answer " + id.ToString() + " from " + site);
            }

            path = Path.Combine(postsdir, "A" + id.ToString() + ".md");

            TextWriter wr = new StreamWriter(path, false, Encoding.UTF8);
            using (wr)
            {
                AnswerMarkdown post = AnswerMarkdown.FromJsonData(site, a);
                post.ToMarkdown(wr);
            }

            Console.WriteLine("Success");
        }

        static void SaveUserAnswers(string site, int userid)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);

            SeApiClient client = new SeApiClient(APIURL, site);
            Dictionary<int, object> answers = client.LoadUserAnswers(userid);            

            Console.WriteLine("Saving {0} answers...", answers.Count);

            foreach (int key in answers.Keys)
            {
                path = Path.Combine(postsdir, "A" + key.ToString() + ".md");
                TextWriter wr = new StreamWriter(path, false,Encoding.UTF8);
                using (wr)
                {
                    AnswerMarkdown post = AnswerMarkdown.FromJsonData(site, answers[key]);
                    post.ToMarkdown(wr);
                }
            }            
        }

        static void SaveQuestionsForSavedAnswers(string site, string subdir)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, subdir + "\\");            

            Console.WriteLine("Loading questions of existing answers ({0}, {1})...", site, subdir);

            PostSet posts = PostSet.LoadFromDir(postsdir, site);
            Dictionary<int, Question> questions = posts.Questions;

            Console.WriteLine("Answers without parent question: {0}", posts.SingleAnswers.Count);

            int n = 0;

            foreach (int a in posts.SingleAnswers.Keys)
            {
                try
                {
                    SaveQuestion(site, posts.SingleAnswers[a].DataDynamic.question_id);
                    n++;
                    if (n > 70) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + ": " + ex.Message);
                    System.Threading.Thread.Sleep(20 * 1000);
                }
            }
        }

        static void Generate(string site, string subdir,string toc_title)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, subdir+"\\");
            string htmldir = "..\\..\\..\\..\\html\\" + site + "\\";
            string targetdir = Path.Combine(htmldir, subdir + "\\");
            string path;
            TextWriter wr;

            if (!Directory.Exists(htmldir)) Directory.CreateDirectory(htmldir);
            if (!Directory.Exists(targetdir)) Directory.CreateDirectory(targetdir);  

            Console.WriteLine("Generating HTML files ({0}, {1})...", site, subdir);

            PostSet posts = PostSet.LoadFromDir(postsdir, site);
            Dictionary<int, Question> questions = posts.Questions;
            Console.WriteLine("JSON questions: {0}", questions.Count);                                  

            foreach (int q in questions.Keys)
            {
                string title = questions[q].DataDynamic.title;                

                path = Path.Combine(targetdir, q.ToString() + ".md");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    questions[q].GenerateHTML(wr);
                }
            }

            Console.WriteLine("JSON answers: {0}", posts.SingleAnswers.Count);

            foreach (int a in posts.SingleAnswers.Keys)
            {
                path = Path.Combine(targetdir, a.ToString() + ".md");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    HTML.RenderHeader(a,wr);
                    posts.SingleAnswers[a].GenerateHTML(wr);
                    HTML.RenderBottom(wr);
                }
            }

            Console.WriteLine("Markdown questions: {0}", posts.MarkdownQuestions.Count);

            foreach (int q in posts.MarkdownQuestions.Keys)
            {  
                path = Path.Combine(targetdir, q.ToString() + ".md");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    posts.MarkdownQuestions[q].GenerateHTML(wr);
                }
            }

            Console.WriteLine("Markdown answers: {0}", posts.MarkdownAnswers.Count);

            foreach (int a in posts.MarkdownAnswers.Keys)
            {
                path = Path.Combine(targetdir, a.ToString() + ".md");
                wr = new StreamWriter(path, false);

                using (wr)
                {
                    HTML.RenderHeader(a, wr);
                    posts.MarkdownAnswers[a].GenerateHTML(wr);
                    HTML.RenderBottom(wr);                    
                }
            }

            Console.WriteLine("Generating TOC ({0}: {1})...",site,toc_title);
            path = Path.Combine(targetdir, "index.md");
            wr = new StreamWriter(path, false);
            using (wr)
            {
                HTML.RenderTOC(site,toc_title, posts, wr);
            }

            Console.WriteLine("Generating toc.yml ({0}: {1})...", site, toc_title);
            path = Path.Combine(targetdir, "toc.yml");
            wr = new StreamWriter(path, false);
            using (wr)
            {
                HTML.RenderYamlTOC(site, toc_title, posts, wr);
            }
        }

        static void ConvertToMarkdown(string site, string src, string target)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, src + "\\");
            string targetdir = Path.Combine(datadir, target + "\\");            
                        
            if (!Directory.Exists(targetdir)) Directory.CreateDirectory(targetdir);

            Console.WriteLine("Converting data files to markdown ({0}/{1})...", site, src);

            Dictionary<int, PostMarkdown> posts = PostMarkdown.LoadFromJsonDir(postsdir, site);
            Console.WriteLine("Posts: {0}", posts.Count);
            PostMarkdown.SaveToDir(targetdir, posts.Values);

            Dictionary<int, QuestionMarkdown> questions = QuestionMarkdown.LoadFromJsonDir(postsdir, site);
            Console.WriteLine("Questions: {0}", questions.Count);
            QuestionMarkdown.SaveToDir(targetdir, questions.Values);

            Dictionary<int, AnswerMarkdown> answers = AnswerMarkdown.LoadFromJsonDir(postsdir, site);
            Console.WriteLine("Answers: {0}", answers.Count);
            AnswerMarkdown.SaveToDir(targetdir, answers.Values);
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                LoadDataMarkdown();
                //ConvertToMarkdown("ru.stackoverflow.com", "posts.json", "posts");

                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
            else if (args.Length >= 3 && args[0] == "saveq")
            {
                SaveQuestion(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "savea")
            {
                SaveSingleAnswer(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "saveu")
            {
                SaveUserAnswers(args[1], Convert.ToInt32(args[2]));
                Console.WriteLine("Done");
            }
            else if (args.Length >= 3 && args[0] == "sync")
            {
                SaveQuestionsForSavedAnswers(args[1], args[2]);
                Console.WriteLine("Done");
            }
            else if (args.Length >= 1 && args[0] == "generate")
            {
                Generate("ru.meta.stackoverflow.com", "posts", "Posts");
                Generate("ru.meta.stackoverflow.com", "deleted", "Deleted posts");
                Generate("ru.stackoverflow.com", "posts", "Posts");
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
                        Console.WriteLine(" Pulling latest changes from remote repository: {0}", DateTime.Now);
                        Console.WriteLine(GitBash.ExecuteCommand("cd ../../../../../; git pull"));
                        LoadDataMarkdown();
                        Console.WriteLine("Done");
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
