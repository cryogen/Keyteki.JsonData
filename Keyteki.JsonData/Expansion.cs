namespace Keyteki.JsonData
{
    using System;
    using System.Collections.Generic;

    public class Expansion
    {
        public List<int> Ids { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int CardCount { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}