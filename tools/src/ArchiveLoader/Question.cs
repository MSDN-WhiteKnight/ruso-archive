using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchiveLoader
{
    class Question : Post
    {
        List<Answer> answers = new List<Answer>();

        public Question(string siteval, int idval, object o)
        {
            this.site = siteval;
            this.id = idval;
            this.data = o;
        }

        public void AddAnswer(Answer a)
        {
            this.answers.Add(a);
            a.Parent = this;
        }

        public IEnumerable<Answer> Answers { get { return this.answers; } }

        public static Dictionary<int,Question> LoadAllFromDir(string path,string site)
        {
            Dictionary<int, Question> questions = new Dictionary<int, Question>();
            Dictionary<int, Answer> answers = new Dictionary<int, Answer>();
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
                    Console.WriteLine("Error reading file "+files[i]);
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }

            files = Directory.GetFiles(path, "*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileNameWithoutExtension(files[i]);
                if (file.StartsWith("Q")) continue;                
                int id;

                if (!Int32.TryParse(file, out id))
                {
                    Console.WriteLine("Bad answer id = {0} in file {1}", file, files[i]);
                    continue;
                }

                string json = File.ReadAllText(files[i], Encoding.UTF8);
                Answer a = new Answer(site, id, JSON.Parse(json));
                answers[id] = a;

                int qid = a.DataDynamic.question_id;

                if (questions.ContainsKey(qid))
                {
                    questions[qid].AddAnswer(a);
                }
                else Console.WriteLine("Question witout parent answer: {0}", files[i]);
            }

            return questions;
        }
    }
}
