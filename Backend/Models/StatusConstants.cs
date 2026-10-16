namespace BiDE.Models
{
    /// <summary>
    /// Centralised string constants for the stringly-typed status/enum-like fields
    /// used across the app (Booking.Status, Payment.PaymentStatus,
    /// Availability.AvailabilityStatus, payment methods, and CancelledBy).
    ///
    /// These values are persisted as plain strings to match the existing data and
    /// schema, so they must not change value. Referencing them through these
    /// constants removes scattered magic strings and prevents typos such as
    /// "Canceled" vs "Cancelled".
    /// </summary>
    public static class BookingStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Rejected = "Rejected";
    }

    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string Rejected = "Rejected";
        // Virtual status used in code when a booking has no Payment row yet.
        public const string Unpaid = "Unpaid";
    }

    public static class AvailabilityStatus
    {
        public const string Available = "Available";
        public const string Booked = "Booked";
    }

    public static class PaymentMethod
    {
        public const string Eft = "EFT";
        public const string Cash = "Cash";
        public const string Card = "Card";

        /// <summary>All accepted payment methods, for validation.</summary>
        public static readonly string[] All = { Eft, Cash, Card };
    }

    public static class CancelledBy
    {
        public const string Student = "Student";
        public const string Instructor = "Instructor";
    }
}
