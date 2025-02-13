﻿using Plato.Internal.Abstractions;

namespace Plato.Twitter.Models
{
    public class TwitterSettings : Serializable
    {

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string AccessToken { get; set; }

        public string AccessTokenSecret { get; set; }

    }
}
