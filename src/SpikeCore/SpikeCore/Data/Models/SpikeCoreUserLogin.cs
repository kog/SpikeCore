﻿using Microsoft.AspNetCore.Identity;

namespace SpikeCore.Data.Models
{
    public class SpikeCoreUserLogin : IdentityUserLogin<string>
    {
        public string MetaData { get; set; }
    }
}