﻿using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Plato.Models.Annotations;
using Plato.Abstractions.Extensions;

namespace Plato.Models.Roles
{

    [TableName("Plato_Roles")]
    public class Role : IdentityRole<int>, IModel<Role>
    {

        #region "Public Properties"

        public override int Id { get; set; }

        public override string Name { get; set; }

        public string Description { get; set;  }

        public List<RoleClaim> RoleClaims { get; } = new List<RoleClaim>();
        
        public DateTime? CreatedDate { get; set; }

        public int CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int ModifiedUserId { get; set; }
        
        #endregion

        #region "Constructor"
        public Role()
        {

        }

        public Role(string name)
            :this(0, name)
        {
            
        }

        public Role(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public Role(IDataReader reader)
        {
           PopulateModel(reader);
        }

        #endregion

        #region "Implementation"

        public void PopulateModel(IDataReader dr)
        {

            if (dr.ColumnIsNotNull("Id"))
                Id = Convert.ToInt32(dr["Id"]);
            
            if (dr.ColumnIsNotNull("Name"))
                Name = Convert.ToString(dr["Name"]);

            if (dr.ColumnIsNotNull("NormalizedName"))
                NormalizedName = Convert.ToString(dr["NormalizedName"]);

            if (dr.ColumnIsNotNull("Description"))
                Description = Convert.ToString(dr["Description"]);
            
            if (dr.ColumnIsNotNull("Claims"))
            {
                var claims = Convert.ToString(dr["Claims"]).Deserialize<List<RoleClaim>>();
                if (claims != null)
                {
                    foreach (var claim in claims)
                        this.RoleClaims.Add(claim);
                }
            }

            if (dr.ColumnIsNotNull("CreatedDate"))
                CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

            if (dr.ColumnIsNotNull("CreatedUserId"))
                CreatedUserId = Convert.ToInt32(dr["CreatedUserId"]);

            if (dr.ColumnIsNotNull("ModifiedDate"))
                ModifiedDate = Convert.ToDateTime(dr["ModifiedDate"]);

            if (dr.ColumnIsNotNull("ModifiedUserId"))
                ModifiedUserId = Convert.ToInt32(dr["ModifiedUserId"]);

            if (dr.ColumnIsNotNull("ConcurrencyStamp"))
                ConcurrencyStamp = Convert.ToString(dr["ConcurrencyStamp"]);
            
        }

        public void PopulateModel(Action<Role> action)
        {
            action(this);
        }


        #endregion
        
    }
}