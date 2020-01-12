using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchiveLoader
{
    public class PostSet
    {
        public Dictionary<int, Question> Questions { get; set; }
        public Dictionary<int, Answer> SingleAnswers { get; set; }
        public Dictionary<int, string> MarkdownQuestions { get; set; }
        public Dictionary<int, string> MarkdownAnswers { get; set; }

        protected PostSet() { }

        public static PostSet LoadFromDir(string path, string site)
        {
            Dictionary<int, Question> questions = new Dictionary<int, Question>();
            Dictionary<int, Answer> answers = new Dictionary<int, Answer>();
            Dictionary<int, Answer> single_answers = new Dictionary<int, Answer>();

            Dictionary<int, string> mdquestions =new Dictionary<int,string>();
            Dictionary<int, string> mdanswers = new Dictionary<int, string>();

            string[] files = Directory.GetFiles(path, "Q*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                string idstr = file.Substring(1);
                int id;

                if (!Int32.TryParse(idstr, out id))
                {
                    Console.WriteLine("Bad question id = {0} in file {1}", idstr, files[i]);
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(files[i], Encoding.UTF8);
                    Question q = new Question(site, id, JSON.Parse(json));
                    questions[id] = q;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file " + files[i]);
                    Console.WriteLine(ex.ToString());
                }
            }

            files = Directory.GetFiles(path, "A*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                string idstr = file.Substring(1);
                int id;

                if (!Int32.TryParse(idstr, out id))
                {
                    Console.WriteLine("Bad answer id = {0} in file {1}", idstr, files[i]);
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(files[i], Encoding.UTF8);
                    Answer a = new Answer(site, id, JSON.Parse(json));
                    answers[id] = a;

                    int qid = a.DataDynamic.question_id;

                    if (questions.ContainsKey(qid))
                    {
                        questions[qid].AddAnswer(a);
                    }
                    else
                    {                        
                        single_answers[id] = a;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file " + files[i]);
                    Console.WriteLine(ex.ToString());
                }
            }

            files = Directory.GetFiles(path, "Q*.md");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                string idstr = file.Substring(1);
                int id;

                if (!Int32.TryParse(idstr, out id))
                {
                    Console.WriteLine("Bad question id = {0} in file {1}", idstr, files[i]);
                    continue;
                }

                try
                {
                    string md = File.ReadAllText(files[i], Encoding.UTF8);                    
                    mdquestions[id] = md;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file " + files[i]);
                    Console.WriteLine(ex.ToString());
                }
            }

            files = Directory.GetFiles(path, "A*.md");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                string idstr = file.Substring(1);
                int id;

                if (!Int32.TryParse(idstr, out id))
                {
                    Console.WriteLine("Bad answer id = {0} in file {1}", idstr, files[i]);
                    continue;
                }

                try
                {
                    string md = File.ReadAllText(files[i], Encoding.UTF8);
                    mdanswers[id] = md;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file " + files[i]);
                    Console.WriteLine(ex.ToString());
                }
            }

            //Console.WriteLine("{0} answers without parent question", single_answers.Count);

            return new PostSet { 
                Questions = questions, SingleAnswers = single_answers, MarkdownQuestions = mdquestions, MarkdownAnswers = mdanswers 
            };
        }
    }
}
