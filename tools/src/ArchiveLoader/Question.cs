using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchiveLoader
{
    public class Question : Post
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
            return PostSet.LoadFromDir(path, site).Questions;
        }

        public void GenerateHTML(TextWriter wr)
        {
            dynamic data = this.DataDynamic;            
            string ownerstr = HTML.GetOwnerString(data);

            HTML.RenderHeader(data.title, wr);            
            wr.WriteLine("<p><a href=\"{0}\">Source</a> - by {1}</p>", data.link, ownerstr);
            wr.WriteLine("<blockquote>");
            wr.WriteLine(data.body);
            wr.WriteLine("</blockquote>");

            foreach (Answer a in this.Answers)
            {
                a.GenerateHTML(wr);
            }

            HTML.RenderBottom(wr);
        }
    }
}
