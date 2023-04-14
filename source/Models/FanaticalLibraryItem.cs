using System;
using System.Collections.Generic;

namespace FanaticalLibrary.Models
{
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


    public class FanaticalLibraryItem
    {
        public string _id { get; set; }
        //public string iid { get; set; }
        //public string cover { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        //public string display_type { get; set; }
        //public Drm drm { get; set; }
        //public Platforms platforms { get; set; }
        //public bool giveaway { get; set; }
        //public object[] downloads { get; set; }
        //public string support_link { get; set; }
        //public object genbaId { get; set; }
        //public string zenvaId { get; set; }
        //public Payment payment { get; set; }
        //public string supplier_id { get; set; }
        //public bool isFreeProduct { get; set; }
        //public bool preorder { get; set; }
        public string status { get; set; }
        //public string serialId { get; set; }
        //public DateTime serialExpiry { get; set; }
        //public Report report { get; set; }
        //public object[] bundles { get; set; }
        public Order order { get; set; }
        //public DateTime purchased { get; set; }
        //public bool isGiftRecipient { get; set; }
        //public bool ubisoft_ska { get; set; }
        //public string promotion { get; set; }
    }

    //public class Drm
    //{
    //    public bool drm_free { get; set; }
    //    public bool steam { get; set; }
    //    public bool origin { get; set; }
    //    public bool uplay { get; set; }
    //    public bool rockstar { get; set; }
    //    public bool esonline { get; set; }
    //    public bool oculus { get; set; }
    //    public bool bethesda { get; set; }
    //    public bool epicgames { get; set; }
    //    public bool _switch { get; set; }
    //    public bool threeds { get; set; }
    //    public bool gog { get; set; }
    //    public bool magix { get; set; }
    //    public bool zenva { get; set; }
    //    public bool utalk { get; set; }
    //    public bool redeem { get; set; }
    //    public bool voucher { get; set; }
    //    public bool playstation { get; set; }
    //    public bool roblox { get; set; }
    //    public bool xbox { get; set; }
    //}

    //public class Platforms
    //{
    //    public bool windows { get; set; }
    //    public bool mac { get; set; }
    //    public bool linux { get; set; }
    //}

    //public class Payment
    //{
    //    public int stotal { get; set; }
    //    public int vat { get; set; }
    //    public int total { get; set; }
    //    public int discountPercent { get; set; }
    //    public int discountAmount { get; set; }
    //}

    //public class Report
    //{
    //    public float total { get; set; }
    //    public float vat { get; set; }
    //}

    public class Order
    {
        public string _id { get; set; }
        //public DateTime date { get; set; }
        //public string status { get; set; }
    }

}

