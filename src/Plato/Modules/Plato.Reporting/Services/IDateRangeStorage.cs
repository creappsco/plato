﻿using System;
using Microsoft.AspNetCore.Mvc;

namespace Plato.Reporting.Services
{
    public interface IDateRangeStorage
    {
        DateTimeOffset Start { get; }

        DateTimeOffset End { get; }

        void Set(DateTimeOffset start, DateTimeOffset end);

        RouteValueDateRangeStorage Contextualize(ActionContext context);

    }
}
