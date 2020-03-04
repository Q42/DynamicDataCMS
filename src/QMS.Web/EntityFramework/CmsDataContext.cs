﻿using Microsoft.EntityFrameworkCore;
using QMS.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMS.Web.EntityFramework
{
    public class CmsDataContext : DbContext
    {
        public CmsDataContext(DbContextOptions<CmsDataContext> options)
            : base(options)
        {
        }


        public DbSet<Student> Students { get; set; }
        public DbSet<Book> Book { get; set; }

    }
}