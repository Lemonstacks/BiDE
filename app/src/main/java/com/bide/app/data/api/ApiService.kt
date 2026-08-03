package com.bide.app.data.api

import com.bide.app.data.models.*
import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // --- Auth ---
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    @POST("api/auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    // --- Instructors (public + student) ---
    @GET("api/instructors")
    suspend fun getInstructors(
        @Query("search") search: String? = null,
        @Query("specialization") specialization: String? = null
    ): Response<List<InstructorListItem>>

    @GET("api/instructors/{id}")
    suspend fun getInstructorDetail(@Path("id") id: Int): Response<InstructorDetail>

    @POST("api/instructors/book")
    suspend fun bookLesson(@Body request: CreateBookingRequest): Response<BookingDto>

    // --- Student ---
    @GET("api/student/bookings")
    suspend fun getStudentBookings(): Response<StudentBookingsResponse>

    @POST("api/student/bookings/{bookingId}/cancel")
    suspend fun cancelBooking(
        @Path("bookingId") bookingId: Int,
        @Body request: CancelBookingRequest
    ): Response<MessageResponse>

    @POST("api/student/bookings/{bookingId}/review")
    suspend fun leaveReview(
        @Path("bookingId") bookingId: Int,
        @Body request: LeaveReviewRequest
    ): Response<MessageResponse>

    // --- Instructor Dashboard ---
    @GET("api/instructor-dashboard")
    suspend fun getInstructorDashboard(): Response<InstructorDashboardResponse>

    @POST("api/instructor-dashboard/bookings/{bookingId}/accept")
    suspend fun acceptBooking(@Path("bookingId") bookingId: Int): Response<MessageResponse>

    @POST("api/instructor-dashboard/bookings/{bookingId}/reject")
    suspend fun rejectBooking(
        @Path("bookingId") bookingId: Int,
        @Body request: RejectBookingRequest
    ): Response<MessageResponse>

    @POST("api/instructor-dashboard/bookings/{bookingId}/complete")
    suspend fun completeBooking(@Path("bookingId") bookingId: Int): Response<MessageResponse>

    @GET("api/instructor-dashboard/availability")
    suspend fun getAvailability(): Response<List<AvailabilitySlot>>

    @POST("api/instructor-dashboard/availability")
    suspend fun addAvailability(@Body request: AddAvailabilityRequest): Response<AvailabilitySlot>

    @DELETE("api/instructor-dashboard/availability/{id}")
    suspend fun deleteAvailability(@Path("id") id: Int): Response<MessageResponse>

    @GET("api/instructor-dashboard/offerings")
    suspend fun getOfferings(): Response<List<Offering>>

    @POST("api/instructor-dashboard/offerings")
    suspend fun createOffering(@Body request: CreateOfferingRequest): Response<Offering>

    @PUT("api/instructor-dashboard/offerings/{offerId}")
    suspend fun updateOffering(
        @Path("offerId") offerId: Int,
        @Body request: UpdateOfferingRequest
    ): Response<Offering>

    @DELETE("api/instructor-dashboard/offerings/{offerId}")
    suspend fun deleteOffering(@Path("offerId") offerId: Int): Response<MessageResponse>

    @POST("api/instructor-dashboard/bookings/{bookingId}/progress")
    suspend fun addProgress(
        @Path("bookingId") bookingId: Int,
        @Body request: AddProgressRequest
    ): Response<ProgressDto>

    // --- Profile ---
    @GET("api/profile")
    suspend fun getProfile(): Response<Any>

    @PUT("api/profile/student")
    suspend fun updateStudentProfile(@Body request: UpdateStudentProfileRequest): Response<StudentProfile>

    @PUT("api/profile/instructor")
    suspend fun updateInstructorProfile(@Body request: UpdateInstructorProfileRequest): Response<InstructorProfile>

    @DELETE("api/profile")
    suspend fun deleteProfile(): Response<MessageResponse>
}
