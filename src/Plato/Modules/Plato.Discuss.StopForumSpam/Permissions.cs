﻿using System.Collections.Generic;
using Plato.Internal.Security.Abstractions;

namespace Plato.Discuss.StopForumSpam
{
    public class Permissions : IPermissionsProvider<Permission>
    {

        public static readonly Permission ViewStopForumSpam =
            new Permission("DiscussViewSFS", "Can view  StopForumSpam details");

        public static readonly Permission AddToStopForumSpam =
            new Permission("DiscussAddToSFS", "Can submit spammers to StopForumSpam");
        
        public IEnumerable<Permission> GetPermissions()
        {
            return new[]
            {
                ViewStopForumSpam,
                AddToStopForumSpam
            };
        }

        public IEnumerable<DefaultPermissions<Permission>> GetDefaultPermissions()
        {
            return new[]
            {
                new DefaultPermissions<Permission>
                {
                    RoleName = DefaultRoles.Administrator,
                    Permissions = new[]
                    {
                        ViewStopForumSpam,
                        AddToStopForumSpam
                    }
                },
                new DefaultPermissions<Permission>
                {
                    RoleName = DefaultRoles.Staff,
                    Permissions = new[]
                    {
                        ViewStopForumSpam,
                        AddToStopForumSpam
                    }
                }
            };

        }

    }

}
