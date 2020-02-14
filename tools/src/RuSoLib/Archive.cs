using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using RuSoLib;

namespace RuSoLib
{
    public static class Archive
    {
        public const string APIURL = "https://api.stackexchange.com/2.2/";
        
        public static void SaveQuestion(string site, int id)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, "posts\\");
            string path;

            if (!Directory.Exists(postsdir)) Directory.CreateDirectory(postsdir);

            SeApiClient client = new SeApiClient(APIURL, site);
            string q = client.LoadQuestion(id);

            if (q == null)
            {
                throw new Exception("Failed to load question " + id.ToString() + " from " + site);
            }

            path = Path.Combine(postsdir, "Q" + id.ToString() + ".md");
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

        public static void SaveSingleAnswer(string site, int id)
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

        public static void SaveUserAnswers(string site, int userid)
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
                TextWriter wr = new StreamWriter(path, false, Encoding.UTF8);
                using (wr)
                {
                    AnswerMarkdown post = AnswerMarkdown.FromJsonData(site, answers[key]);
                    post.ToMarkdown(wr);
                }
            }
        }

        public static void SaveQuestionsForSavedAnswers(string site, string subdir)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, subdir + "\\");

            Console.WriteLine("Loading questions of existing answers ({0}, {1})...", site, subdir);

            PostSet posts = PostSet.LoadFromDir(postsdir, site);
            Dictionary<int, Question> questions = posts.Questions;

            Console.WriteLine("Answers without parent question: {0}", posts.MarkdownAnswers.Count);

            int n = 0;

            foreach (int a in posts.MarkdownAnswers.Keys)
            {
                try
                {
                    SaveQuestion(site, posts.MarkdownAnswers[a].QuestionId);
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

        public static void Generate(string site, string subdir, string toc_title)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, subdir + "\\");
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
                    HTML.RenderHeader(a, wr);
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
                    HTML.RenderHeader(posts.MarkdownAnswers[a].Title, wr);
                    posts.MarkdownAnswers[a].GenerateHTML(wr);
                    HTML.RenderBottom(wr);
                }
            }

            Console.WriteLine("Generating TOC ({0}: {1})...", site, toc_title);
            path = Path.Combine(targetdir, "index.md");
            wr = new StreamWriter(path, false);
            using (wr)
            {
                HTML.RenderTOC(site, toc_title, posts, wr);
            }

            Console.WriteLine("Generating toc.yml ({0}: {1})...", site, toc_title);
            path = Path.Combine(targetdir, "toc.yml");
            wr = new StreamWriter(path, false);
            using (wr)
            {
                HTML.RenderYamlTOC(site, toc_title, posts, wr);
            }
        }

        public static void ConvertToMarkdown(string site, string src, string target)
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

        public static void SelectUserAnswers(string site, string subdir, string target, int userid)
        {
            string datadir = "..\\..\\..\\..\\data\\" + site + "\\";
            string postsdir = Path.Combine(datadir, subdir + "\\");
            string targetdir = Path.Combine(datadir, target + "\\");            
            string path;            
                        
            if (!Directory.Exists(targetdir)) Directory.CreateDirectory(targetdir);

            Console.WriteLine("Copying answers of user {0} to {1}...", userid, targetdir);

            PostSet posts = PostSet.LoadFromDir(postsdir, site);
            
            Console.WriteLine("Answers: {0}", posts.AllMarkdownAnswers.Count);

            int c = 0;

            foreach (int a in posts.AllMarkdownAnswers.Keys)
            {
                AnswerMarkdown answer = posts.AllMarkdownAnswers[a];
                if (answer.UserId.Trim() != userid.ToString().Trim()) continue;

                try
                {
                    QuestionMarkdown question = answer.Parent;

                    if (question != null)
                    {
                        if (String.IsNullOrEmpty(question.Title)) question.Title = "Question " + answer.QuestionId.ToString();

                        answer.Title = "Ответ на \"" + question.Title.ToString() + "\"";
                    }

                    path = Path.Combine(targetdir, "A" + a.ToString() + ".md");

                    using (TextWriter wr = new StreamWriter(path, false, Encoding.UTF8))
                    {
                        answer.ToMarkdown(wr);
                    }

                    c++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error on anser " + a.ToString());
                    Console.WriteLine(ex.GetType() + ": " + ex.Message);
                }
            }

            Console.WriteLine("Copied: {0}", c);
        }

    }
}
