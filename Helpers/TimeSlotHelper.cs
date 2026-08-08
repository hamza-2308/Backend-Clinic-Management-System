using System.Globalization;

namespace ClinicApi.Helpers;

public static class TimeSlotHelper
{
    /// <summary>
    /// Parses display strings like "09:00 AM" or "14:30" into a TimeSpan.
    /// </summary>
    public static TimeSpan ParseTimeSlot(string timeSlot)
    {
        var formats = new[] { "h:mm tt", "hh:mm tt", "HH:mm", "H:mm" };

        if (DateTime.TryParseExact(timeSlot.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
        {
            return parsed.TimeOfDay;
        }

        if (TimeSpan.TryParse(timeSlot, out var ts))
        {
            return ts;
        }

        throw new FormatException($"Could not parse time slot '{timeSlot}'. Expected format like '09:00 AM'.");
    }

    /// <summary>
    /// Formats a TimeSpan back into a display string like "09:00 AM".
    /// </summary>
    public static string FormatTimeSlot(TimeSpan time)
    {
        var dt = DateTime.Today.Add(time);
        return dt.ToString("h:mm tt", CultureInfo.InvariantCulture);
    }
}
