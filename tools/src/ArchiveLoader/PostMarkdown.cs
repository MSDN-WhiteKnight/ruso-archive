using System;
using System.Collections.Generic;
using System.Text;

namespace ArchiveLoader
{
    public class PostMarkdown : Post
    {
        public string PostType { get; set; }
        public string Title { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserLink { get; set; }
        public string Link { get; set; }
        public string Body { get; set; }
        public int Score { get; set; }
    }
}
