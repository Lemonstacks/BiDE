package com.bide.app.navigation

object Routes {
    // Auth
    const val LOGIN = "login"
    const val REGISTER = "register"

    // Student
    const val STUDENT_HOME = "student/home"
    const val STUDENT_BOOKINGS = "student/bookings"
    const val INSTRUCTOR_DETAIL = "student/instructor/{instructorId}"
    const val STUDENT_PROFILE = "student/profile"

    // Instructor
    const val INSTRUCTOR_DASHBOARD = "instructor/dashboard"
    const val INSTRUCTOR_AVAILABILITY = "instructor/availability"
    const val INSTRUCTOR_OFFERINGS = "instructor/offerings"
    const val INSTRUCTOR_PROGRESS = "instructor/progress"
    const val INSTRUCTOR_PROFILE = "instructor/profile"

    // Admin
    const val ADMIN_DASHBOARD = "admin/dashboard"

    fun instructorDetail(instructorId: Int) = "student/instructor/$instructorId"
}
