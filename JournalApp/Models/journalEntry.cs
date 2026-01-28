using SQLite;
using System;

namespace JournalApp.Models
{
    public class JournalEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public DateTime EntryDate { get; set; }

        public string? Title { get; set; }
        public string? Content { get; set; }

        public string? PrimaryMood { get; set; } 
        public string? SecondaryMoods { get; set; } 

        // ADDED: Requirement for "Organize entries under categories"
        public string? Category { get; set; } = "Personal"; 

        public string? Tags { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now; 
    }
}