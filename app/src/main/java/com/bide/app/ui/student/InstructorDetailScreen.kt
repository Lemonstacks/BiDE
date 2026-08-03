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
import com.bide.app.data.models.CreateBookingRequest
import com.bide.app.data.models.InstructorDetail
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InstructorDetailScreen(
    apiService: ApiService,
    instructorId: Int,
    onBack: () -> Unit
) {
    var instructor by remember { mutableStateOf<InstructorDetail?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var selectedOfferId by remember { mutableStateOf<Int?>(null) }
    var selectedScheduleId by remember { mutableStateOf<Int?>(null) }
    var bookingMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(instructorId) {
        try {
            val response = apiService.getInstructorDetail(instructorId)
            if (response.isSuccessful) {
                instructor = response.body()
            }
        } catch (_: Exception) {}
        isLoading = false
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(instructor?.let { "${it.firstName} ${it.lastName}" } ?: "Instructor") },
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
        } else if (instructor == null) {
            Box(modifier = Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                Text("Instructor not found.")
            }
        } else {
            val inst = instructor!!
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                // Info
                item {
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Column(modifier = Modifier.padding(16.dp)) {
                            Text("Experience: ${inst.experience} years")
                            if (inst.certification != null) Text("Certification: ${inst.certification}")
                            if (inst.suburb != null) Text("Location: ${inst.suburb}")
                            Text("Contact: ${inst.email}")
                        }
                    }
                }

                // Offerings
                item {
                    Text("Lesson Offerings", style = MaterialTheme.typography.titleMedium)
                }
                items(inst.offerings) { offering ->
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        colors = if (selectedOfferId == offering.offerId)
                            CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
                        else CardDefaults.cardColors()
                    ) {
                        Column(modifier = Modifier.padding(12.dp)) {
                            Text(offering.title, style = MaterialTheme.typography.bodyLarge)
                            Text("${offering.lessonType} - R${offering.price}")
                            if (offering.description != null) Text(offering.description, style = MaterialTheme.typography.bodySmall)
                            TextButton(onClick = { selectedOfferId = offering.offerId }) {
                                Text(if (selectedOfferId == offering.offerId) "Selected" else "Select")
                            }
                        }
                    }
                }

                // Availability
                item {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text("Available Slots", style = MaterialTheme.typography.titleMedium)
                }
                items(inst.availability) { slot ->
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        colors = if (selectedScheduleId == slot.availabilityId)
                            CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
                        else CardDefaults.cardColors()
                    ) {
                        Row(
                            modifier = Modifier.padding(12.dp).fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text("${slot.date.take(10)} | ${slot.startTime} - ${slot.endTime}")
                            TextButton(onClick = { selectedScheduleId = slot.availabilityId }) {
                                Text(if (selectedScheduleId == slot.availabilityId) "Selected" else "Select")
                            }
                        }
                    }
                }

                // Book button
                item {
                    Spacer(modifier = Modifier.height(16.dp))
                    Button(
                        onClick = {
                            if (selectedOfferId != null && selectedScheduleId != null) {
                                scope.launch {
                                    try {
                                        val res = apiService.bookLesson(
                                            CreateBookingRequest(instructorId, selectedOfferId!!, selectedScheduleId!!)
                                        )
                                        bookingMessage = if (res.isSuccessful) "Booking submitted!" else "Booking failed."
                                    } catch (_: Exception) {
                                        bookingMessage = "Connection error."
                                    }
                                }
                            }
                        },
                        modifier = Modifier.fillMaxWidth(),
                        enabled = selectedOfferId != null && selectedScheduleId != null
                    ) { Text("Book Lesson") }

                    if (bookingMessage != null) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(bookingMessage!!, color = MaterialTheme.colorScheme.primary)
                    }
                }

                // Reviews
                if (inst.reviews.isNotEmpty()) {
                    item {
                        Spacer(modifier = Modifier.height(16.dp))
                        Text("Reviews", style = MaterialTheme.typography.titleMedium)
                    }
                    items(inst.reviews) { review ->
                        Card(modifier = Modifier.fillMaxWidth()) {
                            Column(modifier = Modifier.padding(12.dp)) {
                                Text("${review.studentName} - ${review.rating}/5")
                                if (review.comment != null) Text(review.comment, style = MaterialTheme.typography.bodySmall)
                            }
                        }
                    }
                }

                item { Spacer(modifier = Modifier.height(24.dp)) }
            }
        }
    }
}
