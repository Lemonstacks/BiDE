package com.bide.app.data.models

data class StudentProfile(
    val studentId: Int,
    val firstName: String,
    val lastName: String,
    val email: String,
    val contact: String,
    val suburb: String?,
    val createdAt: String
)

data class InstructorProfile(
    val instructorId: Int,
    val firstName: String,
    val lastName: String,
    val email: String,
    val contact: String,
    val suburb: String?,
    val certification: String?,
    val experience: Int,
    val status: String,
    val isVerified: Boolean,
    val createdAt: String
)

data class UpdateStudentProfileRequest(
    val firstName: String,
    val lastName: String,
    val contact: String,
    val suburb: String?
)

data class UpdateInstructorProfileRequest(
    val firstName: String,
    val lastName: String,
    val contact: String,
    val suburb: String?,
    val certification: String?,
    val experience: Int
)
