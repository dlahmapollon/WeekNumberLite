using System;

namespace WeekNumberLite
{
    internal static class Week
    {
        /// <summary>
        /// ISO 8601 week number: weeks start on Monday, week 1 contains the first Thursday of the year.
        /// </summary>
        internal static int Of(DateTime date)
        {
            // An ISO week belongs entirely to the year of its Thursday.
            DateTime thursday = date.Date.AddDays(3 - ((int)date.DayOfWeek + 6) % 7);
            return (thursday.DayOfYear - 1) / 7 + 1;
        }
    }
}
