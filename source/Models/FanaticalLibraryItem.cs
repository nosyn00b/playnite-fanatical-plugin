using System;
using System.Collections.Generic;

namespace FanaticalLibrary.Models
{
    public class FanaticalLibraryItem
    {
        public string _id;
        //public string iid;
        //public string cover;
        public string name;
        public string type;
        //public string display_type;
        public Dictionary<string, bool> drm;
        public Dictionary<string, bool> platforms;
        public DateTime purchased;
        public string status;
        public Dictionary<string, string> order;

    }

    public class FanaticalUserTraits
    {
        public string _id;
        public string email;
    }


    public class FanaticalToken
    {
        public bool authenticated;
        public string error;
        public string token;
    }
}

