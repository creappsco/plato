﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Plato.Environment.Modules
{
    public interface IModuleLoaderService
    {
        List<Assembly> LoadModule(ModuleDescriptor descriptor);      
        
    }
}
