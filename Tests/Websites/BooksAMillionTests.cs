using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests.Websites
{
    public class BooksAMillionTests
    {
        [SetUp]
        public void Setup()
        {
            BooksAMillion.ClearData();
        }
    }
}