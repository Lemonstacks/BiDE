package com.bide.app.ui.student

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.bide.app.data.api.ApiService
import com.bide.app.data.api.TokenManager
import com.bide.app.data.models.InstructorListItem
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BrowseInstructorsScreen(
    apiService: ApiService,
    onInstructorClick: (Int) -> Unit,
    onNavigateToBookings: () -> Unit,
    onLogout: () -> Unit,
    tokenManager: TokenManager
) {
    var instructors by remember { mutableStateOf<List<InstructorListItem>>(emptyList()) }
    var searchQuery by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(true) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        try {
            val response = apiService.getInstructors()
            if (response.isSuccessful) {
                instructors = response.body() ?: emptyList()
            }
        } catch (_: Exception) {}
        isLoading = false
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Find Instructors") },
                actions = {
                    TextButton(onClick = onNavigateToBookings) { Text("My Bookings") }
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
        Column(modifier = Modifier.fillMaxSize().padding(padding)) {
            // Search
            OutlinedTextField(
                value = searchQuery,
                onValueChange = { searchQuery = it },
                label = { Text("Search by name or suburb") },
                leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) },
                modifier = Modifier.fillMaxWidth().padding(16.dp),
                singleLine = true
            )

            // Search button
            Button(
                onClick = {
                    scope.launch {
                        isLoading = true
                        try {
                            val response = apiService.getInstructors(search = searchQuery.ifBlank { null })
                            if (response.isSuccessful) {
                                instructors = response.body() ?: emptyList()
                            }
                        } catch (_: Exception) {}
                        isLoading = false
                    }
                },
                modifier = Modifier.padding(horizontal = 16.dp)
            ) { Text("Search") }

            Spacer(modifier = Modifier.height(8.dp))

            if (isLoading) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
            } else if (instructors.isEmpty()) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text("No instructors found.")
                }
            } else {
                LazyColumn(modifier = Modifier.fillMaxSize()) {
                    items(instructors) { instructor ->
                        InstructorCard(instructor, onClick = { onInstructorClick(instructor.instructorId) })
                    }
                }
            }
        }
    }
}

@Composable
fun InstructorCard(instructor: InstructorListItem, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 6.dp)
            .clickable(onClick = onClick)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = "${instructor.firstName} ${instructor.lastName}",
                style = MaterialTheme.typography.titleMedium
            )
            if (instructor.certification != null) {
                Text(
                    text = instructor.certification,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }
            Row {
                if (instructor.suburb != null) {
                    Text(text = instructor.suburb, style = MaterialTheme.typography.bodySmall)
                    Spacer(modifier = Modifier.width(16.dp))
                }
                Text(
                    text = "${instructor.experience} yrs experience",
                    style = MaterialTheme.typography.bodySmall
                )
            }
        }
    }
}
