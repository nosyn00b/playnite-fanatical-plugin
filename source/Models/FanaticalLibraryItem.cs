﻿namespace FanaticalLibrary.Models
{
    public class FanaticalLibraryItem
    {
        public string _id;
        public string iid;
        public string cover;
        public string name;
        public string type;
        public string display_type;
        //public List<bool> drm; //TODO
        //public List<bool> platforms; //TODO
        public string purchased;
        public string status;
        //public List<string> order;
    }

    public class FanaticalUserTraits
    {
        public string _id;
        public string email;
    }


    public class FanaticalToken
    {
        public bool authenticated;
        //public string email;
        public string error;
        //public bool sentEmail;
        //public string challenge;
        //public string magicSuccess;
        //public string magicSummoned;
        //public string _id;
        //public string role;
        //public string created;
        //public string language; // ": {"code": "en","label": "English","nativeLabel": "English"}
        //public bool email_confirmed;
        //public bool twoFactorEnabled;
        //public bool email_newsletter;
        //public string steam;// { },
        //public string epic;// { },
        //public bool wishlist_notifications;
        //public bool cart_notifications;
        //public bool review_reminders;
        //public bool user_review_reminders;
        //public bool date_last_email_redeem_confirm;
        //public bool alreadyHasAccount;
        //public string billing;// {"customerName": null,"address1": null,"address2": null,"locality": null,"administrativeArea": null,"postalCode": null,"countryCode": null},
        public string token;
    }
}

