using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveLoader
{
    public class Answer:Post
    {        
        public Answer(string siteval, int idval, object o)
        {
            this.site = siteval;
            this.id = idval;
            this.data = o;
        }

        public int QuestionId { get { return this.DataDynamic.question_id; } }
        public Question Parent { get; set; }

        public void GenerateHTML(TextWriter wr)
        {
            dynamic data = this.DataDynamic;
            dynamic owner = JSON.GetPropertyValue(data, "owner");
            string ownerstr;

            if (owner != null) ownerstr = String.Format("<a href=\"{0}\">{1}</a>", owner.link, owner.display_name);
            else ownerstr = "(unknown person)";
                        
            wr.WriteLine("<h2>Answer {0}</h2>", data.answer_id);
            wr.WriteLine("<p><a href=\"https://{0}/a/{1}/\">Source</a> - by {2}</p>", this.site, data.answer_id, ownerstr);
            wr.WriteLine("<blockquote>");
            wr.WriteLine(data.body);
            wr.WriteLine("</blockquote>");             
        }
    }
}
