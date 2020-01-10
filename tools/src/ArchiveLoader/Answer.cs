using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveLoader
{
    class Answer:Post
    {        
        public Answer(string siteval, int idval, object o)
        {
            this.site = siteval;
            this.id = idval;
            this.data = o;
        }

        public int QuestionId { get { return this.DataDynamic.question_id; } }
        public Question Parent { get; set; }
    }
}
