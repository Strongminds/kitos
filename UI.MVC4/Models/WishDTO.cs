﻿namespace UI.MVC4.Models
{
    public class WishDTO
    {
        public int Id { get; set; }
        public bool IsPublic { get; set; }
        public string Text { get; set; }
        public UserDTO User { get; set; }
    }
}