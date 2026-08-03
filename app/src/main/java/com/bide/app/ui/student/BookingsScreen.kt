package com.bide.app.ui.student

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.bide.app.data.api.ApiService
import com.bide.app.data.models.BookingDto
import com.bide.app.data.models.CancelBookingRequest
import com.bide.app.data.models.LeaveReviewRequest
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BookingsScreen(
    apiService: ApiService,
    onBack: () -> Unit
) {
    var activeBookings by remember { mutableStateOf<List<BookingDto>>(emptyList()) }
    var pastBookings by remember { mutableStateOf<List<BookingDto>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    val scope = rememberCoroutineScope()

    fun loadBookings() {
        scope.launch {
            isLoading = true
            try {
                val response = apiService.getStudentBookings()
                if (response.isSuccessful) {
                    val body = response.body()!!
                    activeBookings = body.active
                    pastBookings = body.past
                }
            } catch (_: Exception) {}
            isLoading = false
        }
    }

    LaunchedEffect(Unit) { loadBookings() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Bookings") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
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
                item {
                    Text("Active Bookings", style = MaterialTheme.typography.titleMedium)
                    if (activeBookings.isEmpty()) Text("No active bookings.", style = MaterialTheme.typography.bodySmall)
                }
                items(activeBookings) { booking ->
                    BookingCard(booking, apiService, onRefresh = { loadBookings() }, isActive = true)
                }

                item {
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("Past Bookings", style = MaterialTheme.typography.titleMedium)
                    if (pastBookings.isEmpty()) Text("No past bookings.", style = MaterialTheme.typography.bodySmall)
                }
                items(pastBookings) { booking ->
                    BookingCard(booking, apiService, onRefresh = { loadBookings() }, isActive = false)
                }

                item { Spacer(modifier = Modifier.height(24.dp)) }
            }
        }
    }
}

@Composable
fun BookingCard(
    booking: BookingDto,
    apiService: ApiService,
    onRefresh: () -> Unit,
    isActive: Boolean
) {
    var showCancelDialog by remember { mutableStateOf(false) }
    var showReviewDialog by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text(booking.lessonTitle, style = MaterialTheme.typography.titleSmall)
            Text("Instructor: ${booking.instructorName}")
            Text("Date: ${booking.scheduleDate.take(10)} | ${booking.startTime} - ${booking.endTime}")
            Text("Status: ${booking.status}", color = MaterialTheme.colorScheme.primary)

            if (isActive) {
                TextButton(onClick = { showCancelDialog = true }) { Text("Cancel") }
            }

            if (booking.status == "Completed" && booking.review == null) {
                TextButton(onClick = { showReviewDialog = true }) { Text("Leave Review") }
            }

            if (booking.review != null) {
                Text("Your rating: ${booking.review.rating}/5", style = MaterialTheme.typography.bodySmall)
            }
        }
    }

    // Cancel dialog
    if (showCancelDialog) {
        var reason by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { showCancelDialog = false },
            title = { Text("Cancel Booking") },
            text = {
                OutlinedTextField(
                    value = reason,
                    onValueChange = { reason = it },
                    label = { Text("Reason (optional)") }
                )
            },
            confirmButton = {
                TextButton(onClick = {
                    scope.launch {
                        apiService.cancelBooking(booking.bookingId, CancelBookingRequest(reason.ifBlank { null }))
                        showCancelDialog = false
                        onRefresh()
                    }
                }) { Text("Confirm") }
            },
            dismissButton = {
                TextButton(onClick = { showCancelDialog = false }) { Text("Back") }
            }
        )
    }

    // Review dialog
    if (showReviewDialog) {
        var rating by remember { mutableStateOf(5) }
        var comment by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { showReviewDialog = false },
            title = { Text("Leave a Review") },
            text = {
                Column {
                    Text("Rating: $rating/5")
                    Slider(
                        value = rating.toFloat(),
                        onValueChange = { rating = it.toInt() },
                        valueRange = 1f..5f,
                        steps = 3
                    )
                    OutlinedTextField(
                        value = comment,
                        onValueChange = { comment = it },
                        label = { Text("Comment (optional)") }
                    )
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    scope.launch {
                        apiService.leaveReview(booking.bookingId, LeaveReviewRequest(rating, comment.ifBlank { null }))
                        showReviewDialog = false
                        onRefresh()
                    }
                }) { Text("Submit") }
            },
            dismissButton = {
                TextButton(onClick = { showReviewDialog = false }) { Text("Back") }
            }
        )
    }
}
