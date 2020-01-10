using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchiveLoader
{
    abstract class Post
    {
        protected string site;
        protected int id;
        protected object data;               

        public string Site { get { return site; } }
        public int Id { get { return id; } }
        public object Data { get { return data; } }
        public dynamic DataDynamic { get { return data as dynamic; } }

        
    }
}
