package com.bide.app.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.bide.app.data.api.ApiService
import com.bide.app.data.api.TokenManager
import com.bide.app.ui.auth.LoginScreen
import com.bide.app.ui.auth.RegisterScreen
import com.bide.app.ui.instructor.AvailabilityScreen
import com.bide.app.ui.instructor.InstructorDashboardScreen
import com.bide.app.ui.instructor.OfferingsScreen
import com.bide.app.ui.student.BookingsScreen
import com.bide.app.ui.student.BrowseInstructorsScreen
import com.bide.app.ui.student.InstructorDetailScreen

@Composable
fun NavGraph(
    navController: NavHostController,
    apiService: ApiService,
    tokenManager: TokenManager,
    startDestination: String
) {
    NavHost(navController = navController, startDestination = startDestination) {

        // --- Auth ---
        composable(Routes.LOGIN) {
            LoginScreen(
                apiService = apiService,
                tokenManager = tokenManager,
                onLoginSuccess = { role ->
                    val dest = when (role) {
                        "Student" -> Routes.STUDENT_HOME
                        "Instructor" -> Routes.INSTRUCTOR_DASHBOARD
                        "Admin" -> Routes.ADMIN_DASHBOARD
                        else -> Routes.LOGIN
                    }
                    navController.navigate(dest) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                },
                onNavigateToRegister = {
                    navController.navigate(Routes.REGISTER)
                }
            )
        }

        composable(Routes.REGISTER) {
            RegisterScreen(
                apiService = apiService,
                tokenManager = tokenManager,
                onRegisterSuccess = { role ->
                    val dest = when (role) {
                        "Student" -> Routes.STUDENT_HOME
                        "Instructor" -> Routes.INSTRUCTOR_DASHBOARD
                        else -> Routes.LOGIN
                    }
                    navController.navigate(dest) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                },
                onNavigateToLogin = {
                    navController.popBackStack()
                }
            )
        }

        // --- Student ---
        composable(Routes.STUDENT_HOME) {
            BrowseInstructorsScreen(
                apiService = apiService,
                onInstructorClick = { id ->
                    navController.navigate(Routes.instructorDetail(id))
                },
                onNavigateToBookings = {
                    navController.navigate(Routes.STUDENT_BOOKINGS)
                },
                onLogout = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(0) { inclusive = true }
                    }
                },
                tokenManager = tokenManager
            )
        }

        composable(
            route = Routes.INSTRUCTOR_DETAIL,
            arguments = listOf(navArgument("instructorId") { type = NavType.IntType })
        ) { backStack ->
            val instructorId = backStack.arguments?.getInt("instructorId") ?: 0
            InstructorDetailScreen(
                apiService = apiService,
                instructorId = instructorId,
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.STUDENT_BOOKINGS) {
            BookingsScreen(
                apiService = apiService,
                onBack = { navController.popBackStack() }
            )
        }

        // --- Instructor ---
        composable(Routes.INSTRUCTOR_DASHBOARD) {
            InstructorDashboardScreen(
                apiService = apiService,
                onNavigateToAvailability = {
                    navController.navigate(Routes.INSTRUCTOR_AVAILABILITY)
                },
                onNavigateToOfferings = {
                    navController.navigate(Routes.INSTRUCTOR_OFFERINGS)
                },
                onLogout = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(0) { inclusive = true }
                    }
                },
                tokenManager = tokenManager
            )
        }

        composable(Routes.INSTRUCTOR_AVAILABILITY) {
            AvailabilityScreen(
                apiService = apiService,
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.INSTRUCTOR_OFFERINGS) {
            OfferingsScreen(
                apiService = apiService,
                onBack = { navController.popBackStack() }
            )
        }

        // --- Admin (placeholder) ---
        composable(Routes.ADMIN_DASHBOARD) {
            // TODO: Admin dashboard screen
        }
    }
}
