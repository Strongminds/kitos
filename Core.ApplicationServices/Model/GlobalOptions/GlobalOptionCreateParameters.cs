﻿namespace Core.ApplicationServices.Model.GlobalOptions
{
    public class GlobalOptionCreateParameters
    {
        public string Name { get; set; }
        public bool IsObligatory { get; set; }
        public string Description { get; set; }
    }
}
