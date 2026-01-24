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

        // MARKING SCHEME ITEM 3: Primary + Secondary Moods
        public string? PrimaryMood { get; set; } 
        public string? SecondaryMoods { get; set; } // Store as comma-separated string: "Happy,Relaxed"

        // MARKING SCHEME ITEM 4: Tags
        public string? Tags { get; set; } // Store as "Work,Health"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now; 
    }
}