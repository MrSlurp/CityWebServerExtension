﻿using System;
using ICities;

namespace CWS_MrSlurpExtensions
{
    public class UserModInfo : IUserMod
    {
        public String Name
        {
            get { return "Mr Slurp's Web Server Extension"; }
        }

        public String Description
        {
            get { return "Add a district and a transport view to Rychard's City Web server (Rychard's mod is required)"; }
        }
    }
}