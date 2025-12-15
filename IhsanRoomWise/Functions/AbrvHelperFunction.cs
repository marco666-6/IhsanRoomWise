// Functions\AbrvHelperFunction.cs

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IhsanRoomWise.Functions
{
    public static class AbrvHelperFunction
    {
        public static string GenerateRoomCode(string roomName, string plantName, int block, int floor)
        {
            string abbreviation = GenerateAbbreviation(roomName);
            string plantNumber = ExtractPlantNumber(plantName);
            
            return $"{abbreviation}-P{plantNumber}-B{block}-F{floor}";
        }

        public static string GenerateAbbreviation(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                return "MR"; // Default: Meeting Room

            // Remove common words that don't add value to abbreviation
            string[] wordsToRemove = { "Room", "Meeting", "Conference", "The", "A", "An" };
            string cleanedName = roomName;

            foreach (string word in wordsToRemove)
            {
                cleanedName = Regex.Replace(cleanedName, $@"\b{word}\b", "", RegexOptions.IgnoreCase);
            }

            // Split by spaces and take first letter of each word
            string[] words = cleanedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder abbreviation = new StringBuilder();

            foreach (string word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    abbreviation.Append(char.ToUpper(word[0]));
                }
            }

            // If abbreviation is empty or too short, use first 2-4 letters of original name
            if (abbreviation.Length == 0)
            {
                string alphanumeric = Regex.Replace(roomName, @"[^a-zA-Z0-9]", "");
                return alphanumeric.Length > 4 
                    ? alphanumeric.Substring(0, 4).ToUpper() 
                    : alphanumeric.ToUpper();
            }

            return abbreviation.ToString();
        }

        public static string ExtractPlantNumber(string plantName)
        {
            if (string.IsNullOrWhiteSpace(plantName))
                return "0";

            // Extract digits from plant name
            string digits = Regex.Replace(plantName, @"[^\d]", "");
            
            return string.IsNullOrEmpty(digits) ? "0" : digits;
        }

        public static string GenerateBookingCode(DateTime bookingDate, int sequenceNumber)
        {
            string dateStr = bookingDate.ToString("yyyyMMdd");
            string sequence = sequenceNumber.ToString("D3"); // Pad with zeros to 3 digits
            
            return $"BK-{dateStr}-{sequence}";
        }

        public static bool IsValidRoomCodeFormat(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                return false;

            // Pattern: ABBREVIATION-P[digit(s)]-B[digit(s)]-F[digit(s)]
            string pattern = @"^[A-Z0-9]+-P\d+-B\d+-F\d+$";
            return Regex.IsMatch(roomCode, pattern);
        }

        public static bool IsValidBookingCodeFormat(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return false;

            // Pattern: BK-YYYYMMDD-XXX
            string pattern = @"^BK-\d{8}-\d{3}$";
            return Regex.IsMatch(bookingCode, pattern);
        }

        public static string GenerateCleanFilename(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "file";

            // Convert to lowercase and replace spaces with hyphens
            string clean = input.ToLower().Trim();
            clean = Regex.Replace(clean, @"[^a-z0-9\s-]", ""); // Remove special chars
            clean = Regex.Replace(clean, @"\s+", "-"); // Replace spaces with hyphens
            clean = Regex.Replace(clean, @"-+", "-"); // Remove duplicate hyphens
            
            return clean;
        }

        public static string GenerateInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            string[] words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return "??";
            
            if (words.Length == 1)
                return words[0].Length > 1 
                    ? words[0].Substring(0, 2).ToUpper() 
                    : words[0].ToUpper();

            // Take first letter of first word and first letter of last word
            return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[words.Length - 1][0])}";
        }

        public static string GeneratePreview(string text, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            // Find the last space before maxLength to avoid cutting words
            int lastSpace = text.LastIndexOf(' ', maxLength);
            
            if (lastSpace > 0)
                return text.Substring(0, lastSpace) + "...";
            
            return text.Substring(0, maxLength) + "...";
        }
    }
}