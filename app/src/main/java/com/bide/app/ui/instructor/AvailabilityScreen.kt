package com.bide.app.ui.instructor

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.bide.app.data.api.ApiService
import com.bide.app.data.models.AddAvailabilityRequest
import com.bide.app.data.models.AvailabilitySlot
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AvailabilityScreen(
    apiService: ApiService,
    onBack: () -> Unit
) {
    var slots by remember { mutableStateOf<List<AvailabilitySlot>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var showAddDialog by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun loadSlots() {
        scope.launch {
            isLoading = true
            try {
                val response = apiService.getAvailability()
                if (response.isSuccessful) slots = response.body() ?: emptyList()
            } catch (_: Exception) {}
            isLoading = false
        }
    }

    LaunchedEffect(Unit) { loadSlots() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Availability") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showAddDialog = true }) {
                Text("+")
            }
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
                if (slots.isEmpty()) {
                    item { Text("No availability slots. Add one!") }
                }
                items(slots) { slot ->
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier.padding(12.dp).fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column {
                                Text("${slot.date.take(10)}")
                                Text("${slot.startTime} - ${slot.endTime}")
                                Text("Status: ${slot.status}", style = MaterialTheme.typography.bodySmall)
                            }
                            if (slot.status != "Booked") {
                                IconButton(onClick = {
                                    scope.launch {
                                        apiService.deleteAvailability(slot.availabilityId)
                                        loadSlots()
                                    }
                                }) {
                                    Icon(Icons.Default.Delete, contentDescription = "Delete")
                                }
                            }
                        }
                    }
                }
                item { Spacer(modifier = Modifier.height(80.dp)) }
            }
        }
    }

    // Add dialog
    if (showAddDialog) {
        var date by remember { mutableStateOf("") }
        var startTime by remember { mutableStateOf("") }
        var endTime by remember { mutableStateOf("") }
        var dialogError by remember { mutableStateOf<String?>(null) }

        AlertDialog(
            onDismissRequest = { showAddDialog = false },
            title = { Text("Add Availability") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = date, onValueChange = { date = it },
                        label = { Text("Date (YYYY-MM-DD)") }, singleLine = true
                    )
                    OutlinedTextField(
                        value = startTime, onValueChange = { startTime = it },
                        label = { Text("Start Time (HH:mm)") }, singleLine = true
                    )
                    OutlinedTextField(
                        value = endTime, onValueChange = { endTime = it },
                        label = { Text("End Time (HH:mm)") }, singleLine = true
                    )
                    if (dialogError != null) {
                        Text(dialogError!!, color = MaterialTheme.colorScheme.error)
                    }
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    scope.launch {
                        try {
                            val res = apiService.addAvailability(
                                AddAvailabilityRequest(date, startTime, endTime)
                            )
                            if (res.isSuccessful) {
                                showAddDialog = false
                                loadSlots()
                            } else {
                                dialogError = "Invalid input. Check date/times."
                            }
                        } catch (_: Exception) {
                            dialogError = "Connection error."
                        }
                    }
                }) { Text("Add") }
            },
            dismissButton = {
                TextButton(onClick = { showAddDialog = false }) { Text("Cancel") }
            }
        )
    }
}
