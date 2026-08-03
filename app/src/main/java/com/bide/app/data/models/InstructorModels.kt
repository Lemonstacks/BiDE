package com.bide.app.data.models

data class InstructorListItem(
    val instructorId: Int,
    val firstName: String,
    val lastName: String,
    val suburb: String?,
    val certification: String?,
    val experience: Int
)

data class InstructorDetail(
    val instructorId: Int,
    val firstName: String,
    val lastName: String,
    val contact: String,
    val email: String,
    val suburb: String?,
    val certification: String?,
    val experience: Int,
    val offerings: List<Offering>,
    val availability: List<AvailabilitySlot>,
    val reviews: List<Review>
)

data class Offering(
    val offerId: Int,
    val title: String,
    val lessonType: String,
    val description: String?,
    val price: Double
)

data class AvailabilitySlot(
    val availabilityId: Int,
    val date: String,
    val startTime: String,
    val endTime: String,
    val status: String
)

data class Review(
    val reviewId: Int,
    val studentName: String,
    val rating: Int,
    val comment: String?,
    val reviewDate: String
)

data class CreateOfferingRequest(
    val title: String,
    val lessonType: String,
    val description: String?,
    val price: Double
)

data class UpdateOfferingRequest(
    val title: String,
    val lessonType: String,
    val description: String?,
    val price: Double
)

data class AddAvailabilityRequest(
    val date: String,
    val startTime: String,
    val endTime: String
)
