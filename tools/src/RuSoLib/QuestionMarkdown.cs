using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RuSoLib
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

        public static new QuestionMarkdown FromJsonData(string siteval, object data)
        {
            QuestionMarkdown res = new QuestionMarkdown();
            res.site = siteval;

            dynamic owner = JSON.GetPropertyValue(data, "owner");

            if (owner != null)
            {
                dynamic owner_link = JSON.GetPropertyValue(owner, "link");
                dynamic owner_id = JSON.GetPropertyValue(owner, "user_id");

                if (owner_link != null)
                {
                    res.UserLink = owner_link;
                }
                else res.UserLink = "";

                if (owner_id != null)
                {
                    res.UserId = owner_id.ToString();
                }
                else res.UserId = "";

                res.UserName = owner.display_name;
            }
            else
            {
                res.UserName = "(unknown person)";
                res.UserLink = "";
                res.UserId = "";
            }
            
            res.PostType = "question";
            res.id = (data as dynamic).question_id;
            res.Title = (data as dynamic).title;
            res.Link = (data as dynamic).link;
            res.Body = (data as dynamic).body;
            res.data = res;
            return res;
        }

        public new void ToMarkdown(TextWriter target)
        {
            target.WriteLine("---");

            string title = this.Title.Replace("\\", "\\\\");
            title = title.Replace("\"", "\\\"");

            target.Write("title: ");
            target.WriteLine("\"" + title + "\"");

            target.Write("se.owner.user_id: ");
            target.WriteLine(this.UserId);
            target.Write("se.owner.display_name: ");
            target.WriteLine("\"" + this.UserName + "\"");
            target.Write("se.owner.link: ");
            target.WriteLine("\"" + this.UserLink + "\"");
            target.Write("se.link: ");
            target.WriteLine("\"" + this.Link + "\"");
            target.Write("se.question_id: ");
            target.WriteLine(this.Id);            
            target.Write("se.post_type: ");
            target.WriteLine(this.PostType);
            target.WriteLine("---");
            target.Write(this.Body);
        }

        public static new QuestionMarkdown FromMarkdown(string siteval, TextReader src)
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

                    if (param_val == "\"\"") param_val = "";

                    if (param_val.Length > 2)
                    {
                        if (param_val[0] == '"') param_val = param_val.Substring(1);
                        if (param_val[param_val.Length - 1] == '"') param_val = param_val.Substring(0, param_val.Length - 1);
                    }

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

            if (HTML.EnableAttribution)
            {
                wr.WriteLine("<p><a href=\"{0}\">Source</a> - by {1}</p>", this.Link, ownerstr);
            }
            else
            {
                wr.WriteLine("<p><a href=\"{0}\">Link</a></p>", this.Link);
            }

            wr.WriteLine("<blockquote>");
            wr.WriteLine(this.Body);
            wr.WriteLine("</blockquote>");

            foreach (AnswerMarkdown a in this.Answers)
            {
                a.GenerateHTML(wr);
            }

            HTML.RenderBottom(wr);
        }

        public static new Dictionary<int, QuestionMarkdown> LoadFromJsonDir(string path, string site)
        {
            Dictionary<int, QuestionMarkdown> posts;

            string[] files = Directory.GetFiles(path, "Q*.json");
            posts = new Dictionary<int, QuestionMarkdown>(files.Length);
            JSON parser = new JSON();

            using (parser)
            {
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
                        posts[id] = QuestionMarkdown.FromJsonData(site, parser.JsonParse(json));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error reading file " + files[i]);
                        Console.WriteLine(ex.ToString());
                    }
                }

            }//end using

            return posts;
        }

        public static void SaveToDir(string dir, IEnumerable<QuestionMarkdown> posts)
        {
            foreach (QuestionMarkdown post in posts)
            {
                string path = Path.Combine(dir, "Q" + post.Id.ToString() + ".md");
                StreamWriter wr = new StreamWriter(path, false, Encoding.UTF8);

                using (wr)
                {
                    post.ToMarkdown(wr);
                }
            }
        }
    }
}
