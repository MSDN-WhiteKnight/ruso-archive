using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ArchiveLoader
{
    public class AnswerMarkdown : PostMarkdown
    {
        public int QuestionId { get; set; }
        public QuestionMarkdown Parent { get; set; }                
        public bool IsAccepted { get; set; }

        public static AnswerMarkdown FromMarkdown(string siteval, TextReader src)
        {
            AnswerMarkdown res = new AnswerMarkdown();
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
                        case "se.is_accepted": res.IsAccepted = Boolean.Parse(param_val); continue;
                        case "se.answer_id": res.id = Int32.Parse(param_val); continue;
                        case "se.question_id": res.QuestionId = Int32.Parse(param_val); continue;
                        case "se.score": res.Score = Int32.Parse(param_val); continue;
                    }
                }
                else if (reading_body)
                {
                    sbBody.AppendLine(line);
                }
            }

            res.Body = sbBody.ToString();
            res.PostType = "answer";
            res.data = res;
            return res;
        }

        public void GenerateHTML(TextWriter wr)
        {
            string ownerstr = HTML.GetOwnerString(this);

            wr.WriteLine("<h2>Answer {0}</h2>", this.Id);
            wr.WriteLine("<p><a href=\"https://{0}/a/{1}/\">Source</a> - by {2}</p>", this.site, this.Id, ownerstr);
            wr.WriteLine("<blockquote>");
            wr.WriteLine(this.Body);
            wr.WriteLine("</blockquote>");
        }

    }
}
