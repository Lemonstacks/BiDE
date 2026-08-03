package com.bide.app.ui.instructor

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.bide.app.data.api.ApiService
import com.bide.app.data.api.TokenManager
import com.bide.app.data.models.BookingDto
import com.bide.app.data.models.RejectBookingRequest
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InstructorDashboardScreen(
    apiService: ApiService,
    onNavigateToAvailability: () -> Unit,
    onNavigateToOfferings: () -> Unit,
    onLogout: () -> Unit,
    tokenManager: TokenManager
) {
    var pending by remember { mutableStateOf<List<BookingDto>>(emptyList()) }
    var accepted by remember { mutableStateOf<List<BookingDto>>(emptyList()) }
    var totalBookings by remember { mutableStateOf(0) }
    var isLoading by remember { mutableStateOf(true) }
    val scope = rememberCoroutineScope()

    fun loadDashboard() {
        scope.launch {
            isLoading = true
            try {
                val response = apiService.getInstructorDashboard()
                if (response.isSuccessful) {
                    val body = response.body()!!
                    pending = body.pending
                    accepted = body.accepted
                    totalBookings = body.totalBookings
                }
            } catch (_: Exception) {}
            isLoading = false
        }
    }

    LaunchedEffect(Unit) { loadDashboard() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Dashboard") },
                actions = {
                    TextButton(onClick = {
                        scope.launch {
                            tokenManager.clearSession()
                            onLogout()
                        }
                    }) { Text("Logout") }
                }
            )
        }
    ) { padding ->
        if (isLoading) {
            Box(modifier = Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                // Stats
                item {
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Column(modifier = Modifier.padding(16.dp)) {
                            Text("Total Bookings: $totalBookings", style = MaterialTheme.typography.titleMedium)
                            Text("Pending: ${pending.size} | Active: ${accepted.size}")
                        }
                    }
                }

                // Quick actions
                item {
                    Spacer(modifier = Modifier.height(8.dp))
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        OutlinedButton(onClick = onNavigateToAvailability, modifier = Modifier.weight(1f)) {
                            Text("Availability")
                        }
                        OutlinedButton(onClick = onNavigateToOfferings, modifier = Modifier.weight(1f)) {
                            Text("Offerings")
                        }
                    }
                }

                // Pending bookings
                item {
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("Pending Bookings", style = MaterialTheme.typography.titleMedium)
                }
                if (pending.isEmpty()) {
                    item { Text("No pending bookings.", style = MaterialTheme.typography.bodySmall) }
                }
                items(pending) { booking ->
                    InstructorBookingCard(booking, apiService, onRefresh = { loadDashboard() })
                }

                // Accepted bookings
                item {
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("Active Lessons", style = MaterialTheme.typography.titleMedium)
                }
                if (accepted.isEmpty()) {
                    item { Text("No active lessons.", style = MaterialTheme.typography.bodySmall) }
                }
                items(accepted) { booking ->
                    InstructorBookingCard(booking, apiService, onRefresh = { loadDashboard() })
                }

                item { Spacer(modifier = Modifier.height(24.dp)) }
            }
        }
    }
}

@Composable
fun InstructorBookingCard(
    booking: BookingDto,
    apiService: ApiService,
    onRefresh: () -> Unit
) {
    val scope = rememberCoroutineScope()

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text(booking.lessonTitle, style = MaterialTheme.typography.titleSmall)
            Text("Student: ${booking.studentName}")
            Text("${booking.scheduleDate.take(10)} | ${booking.startTime} - ${booking.endTime}")
            Text("Status: ${booking.status}")

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                if (booking.status == "Pending") {
                    TextButton(onClick = {
                        scope.launch {
                            apiService.acceptBooking(booking.bookingId)
                            onRefresh()
                        }
                    }) { Text("Accept") }

                    TextButton(onClick = {
                        scope.launch {
                            apiService.rejectBooking(booking.bookingId, RejectBookingRequest(null))
                            onRefresh()
                        }
                    }) { Text("Reject") }
                }

                if (booking.status == "Accepted") {
                    TextButton(onClick = {
                        scope.launch {
                            apiService.completeBooking(booking.bookingId)
                            onRefresh()
                        }
                    }) { Text("Mark Complete") }
                }
            }
        }
    }
}
