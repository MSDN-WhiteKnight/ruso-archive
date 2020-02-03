using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ArchiveLoader
{
    public class QuestionMarkdown : PostMarkdown
    {        
        List<AnswerMarkdown> answers = new List<AnswerMarkdown>();

        public void AddAnswer(AnswerMarkdown a)
        {
            this.answers.Add(a);
            a.Parent = this;
        }

        public IEnumerable<AnswerMarkdown> Answers { get { return this.answers; } }

        public static QuestionMarkdown FromMarkdown(string siteval, TextReader src)
        {
            QuestionMarkdown res = new QuestionMarkdown();
            res.site = siteval;

            bool reading_yml = false;
            bool reading_body = false;
            StringBuilder sbBody = new StringBuilder(5000);

            string param_name;
            string param_val;

            while (true)
            {
                string line = src.ReadLine();
                if (line == null) break;

                if (line == "---" && reading_yml == false && reading_body == false)
                {
                    //start YML block
                    reading_yml = true;
                    continue;
                }

                if (line == "---" && reading_yml == true && reading_body == false)
                {
                    //end YML block
                    reading_yml = false;
                    reading_body = true;
                    continue;
                }

                if (reading_yml)
                {
                    if (line.Length <= 2) continue;
                    int index = line.IndexOf(':');
                    if (index < 0) index = line.Length - 2;

                    param_name = line.Substring(0, index);
                    param_val = line.Substring(index + 1);

                    param_val = param_val.Trim();
                    if (param_val[0] == '"') param_val = param_val.Substring(1);
                    if (param_val[param_val.Length - 1] == '"') param_val = param_val.Substring(0, param_val.Length - 1);
                    param_val = param_val.Replace("\\\"", "\"");
                    param_val = param_val.Replace("\\\\", "\\");

                    switch (param_name)
                    {
                        case "title": res.Title = param_val; continue;
                        case "se.owner.user_id": res.UserId = param_val; continue;
                        case "se.owner.display_name": res.UserName = param_val; continue;
                        case "se.owner.link": res.UserLink = param_val; continue;
                        case "se.link": res.Link = param_val; continue;                        
                        case "se.question_id": res.id = Int32.Parse(param_val); continue;
                        case "se.score": res.Score = Int32.Parse(param_val); continue;
                    }
                }
                else if (reading_body)
                {
                    sbBody.AppendLine(line);
                }
            }

            res.Body = sbBody.ToString();
            res.PostType = "question";
            res.data = res;
            return res;
        }

        public void GenerateHTML(TextWriter wr)
        {            
            string ownerstr = HTML.GetOwnerString(this);

            HTML.RenderHeader(this.Title, wr);
            wr.WriteLine("<p><a href=\"{0}\">Source</a> - by {1}</p>", this.Link, ownerstr);
            wr.WriteLine("<blockquote>");
            wr.WriteLine(this.Body);
            wr.WriteLine("</blockquote>");

            foreach (AnswerMarkdown a in this.Answers)
            {
                a.GenerateHTML(wr);
            }

            HTML.RenderBottom(wr);
        }
    }
}
