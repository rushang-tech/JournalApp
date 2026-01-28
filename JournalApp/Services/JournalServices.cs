using SQLite;
using JournalApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
// iText7 Namespaces
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;

namespace JournalApp.Services
{
    public class JournalService
    {
        private SQLiteAsyncConnection? _db;

        private async Task Init()
        {
            if (_db != null) return;
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyJournal.db");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<JournalEntry>();
        }

        // --- CRUD Operations ---
        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            await Init();
            var start = date.Date;
            var end = start.AddDays(1);
            return await _db!.Table<JournalEntry>()
                            .Where(e => e.EntryDate >= start && e.EntryDate < end)
                            .FirstOrDefaultAsync();
        }

        public async Task SaveEntryAsync(JournalEntry entry)
        {
            await Init();
            if (entry.Id != 0) 
            {
                entry.UpdatedAt = DateTime.Now;
                await _db!.UpdateAsync(entry);
            }
            else 
            {
                entry.CreatedAt = DateTime.Now;
                entry.UpdatedAt = DateTime.Now;
                await _db!.InsertAsync(entry);
            }
        }

        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            await Init();
            return await _db!.Table<JournalEntry>().OrderByDescending(e => e.EntryDate).ToListAsync();
        }

        // --- NEW: Pagination for Requirement #6 ---
        // This method fetches data in chunks from the database
        public async Task<List<JournalEntry>> GetEntriesPagedAsync(int skip, int take)
        {
            await Init();
            return await _db!.Table<JournalEntry>()
                            .OrderByDescending(e => e.EntryDate)
                            .Skip(skip)
                            .Take(take)
                            .ToListAsync();
        }
        
        public async Task DeleteEntryAsync(JournalEntry entry)
        {
            await Init();
            await _db!.DeleteAsync(entry);
        }

        public async Task<List<JournalEntry>> GetEntriesByRangeAsync(DateTime start, DateTime end)
        {
            await Init();
            return await _db!.Table<JournalEntry>()
                .Where(e => e.EntryDate >= start.Date && e.EntryDate <= end.Date)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();
        }

        // --- Analytics ---
        public async Task<int> GetCurrentStreakAsync()
        {
            await Init();
            var allEntries = await _db!.Table<JournalEntry>().OrderByDescending(e => e.EntryDate).ToListAsync();
            if (!allEntries.Any()) return 0;

            int streak = 0;
            var checkDate = DateTime.Today.Date;
            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();

            if (!entryDates.Contains(checkDate) && !entryDates.Contains(checkDate.AddDays(-1))) return 0;
            if (!entryDates.Contains(checkDate)) checkDate = checkDate.AddDays(-1);

            while (entryDates.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            return streak;
        }

        public async Task<int> GetTotalWordsAsync()
        {
            await Init();
            var entries = await _db!.Table<JournalEntry>().ToListAsync();
            return entries.Sum(e => string.IsNullOrEmpty(e.Content) ? 0 : e.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        public async Task<(int longest, int missed)> GetAdvancedStatsAsync()
        {
            var entries = await GetAllEntriesAsync();
            if (!entries.Any()) return (0, 0);

            int longest = 0;
            int current = 0;
            var dates = entries.Select(e => e.EntryDate.Date).Distinct().OrderByDescending(d => d).ToList();

            for (int i = 0; i < dates.Count - 1; i++)
            {
                if ((dates[i] - dates[i+1]).TotalDays == 1)
                {
                    current++;
                    if (current > longest) longest = current;
                }
                else current = 0;
            }

            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var entryDates = entries.Select(e => e.EntryDate.Date).ToHashSet();
            int missed = 0;
            for (var d = thirtyDaysAgo; d <= DateTime.Today; d = d.AddDays(1))
            {
                if (!entryDates.Contains(d)) missed++;
            }

            return (longest + 1, missed);
        }

        // --- iText7 PDF EXPORT ---
        public async Task<string> GeneratePdfExportAsync(DateTime start, DateTime end)
        {
            var entries = await GetEntriesByRangeAsync(start, end);
            if (!entries.Any()) return string.Empty;

            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fileName = $"Journify_Export_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            string filePath = Path.Combine(downloadsPath, fileName);

            using (PdfWriter writer = new PdfWriter(filePath))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document document = new Document(pdf))
            {
                document.Add(new Paragraph("Journify Export")
                    .SetFontSize(24)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                document.Add(new Paragraph($"{start:MMM dd, yyyy} - {end:MMM dd, yyyy}")
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(20));

                foreach (var entry in entries)
                {
                    document.Add(new Paragraph($"{entry.EntryDate:D}  |  Mood: {entry.PrimaryMood}")
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.DARK_GRAY));

                    if (!string.IsNullOrEmpty(entry.Title))
                    {
                        document.Add(new Paragraph(entry.Title)
                            .SetFontSize(16)
                            .SetMarginBottom(5));
                    }

                    // Strip HTML tags for PDF text
                    string cleanContent = System.Text.RegularExpressions.Regex.Replace(entry.Content ?? "", "<.*?>", String.Empty);
                    
                    document.Add(new Paragraph(cleanContent)
                        .SetFontSize(11)
                        .SetMarginBottom(10));

                    if (!string.IsNullOrEmpty(entry.Tags))
                    {
                        document.Add(new Paragraph($"Tags: {entry.Tags}")
                            .SetFontSize(9)
                            .SetFontColor(ColorConstants.GRAY));
                    }

                    document.Add(new Paragraph("___________________________________________________")
                        .SetFontColor(ColorConstants.LIGHT_GRAY)
                        .SetMarginBottom(20));
                }
                
                document.Add(new Paragraph("Generated by Journify")
                    .SetFontSize(8)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginTop(20));
            }

            return filePath;
        }
    }
}