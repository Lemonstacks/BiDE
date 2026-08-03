package com.bide.app.data.models

data class BookingDto(
    val bookingId: Int,
    val instructorName: String,
    val studentName: String,
    val lessonTitle: String,
    val lessonType: String,
    val scheduleDate: String,
    val startTime: String,
    val endTime: String,
    val status: String,
    val createdAt: String,
    val cancellationReason: String?,
    val review: Review?,
    val progress: List<ProgressDto>
)

data class ProgressDto(
    val progressId: Int,
    val progressDate: String,
    val duration: Int,
    val completionStatus: String,
    val feedback: String?
)

data class StudentBookingsResponse(
    val active: List<BookingDto>,
    val past: List<BookingDto>
)

data class CreateBookingRequest(
    val instructorId: Int,
    val offerId: Int,
    val scheduleId: Int
)

data class CancelBookingRequest(
    val reason: String?
)

data class LeaveReviewRequest(
    val rating: Int,
    val comment: String?
)

data class RejectBookingRequest(
    val reason: String?
)

data class AddProgressRequest(
    val feedback: String?,
    val completionStatus: String,
    val duration: Int
)

data class InstructorDashboardResponse(
    val totalBookings: Int,
    val pending: List<BookingDto>,
    val accepted: List<BookingDto>,
    val completed: List<BookingDto>
)

data class MessageResponse(
    val message: String
)
