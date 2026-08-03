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
import com.bide.app.data.models.CreateOfferingRequest
import com.bide.app.data.models.Offering
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OfferingsScreen(
    apiService: ApiService,
    onBack: () -> Unit
) {
    var offerings by remember { mutableStateOf<List<Offering>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var showAddDialog by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun loadOfferings() {
        scope.launch {
            isLoading = true
            try {
                val response = apiService.getOfferings()
                if (response.isSuccessful) offerings = response.body() ?: emptyList()
            } catch (_: Exception) {}
            isLoading = false
        }
    }

    LaunchedEffect(Unit) { loadOfferings() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Offerings") },
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
                if (offerings.isEmpty()) {
                    item { Text("No offerings yet. Create one!") }
                }
                items(offerings) { offering ->
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier.padding(12.dp).fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(offering.title, style = MaterialTheme.typography.titleSmall)
                                Text("${offering.lessonType} - R${offering.price}")
                                if (offering.description != null) {
                                    Text(offering.description, style = MaterialTheme.typography.bodySmall)
                                }
                            }
                            IconButton(onClick = {
                                scope.launch {
                                    apiService.deleteOffering(offering.offerId)
                                    loadOfferings()
                                }
                            }) {
                                Icon(Icons.Default.Delete, contentDescription = "Delete")
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
        var title by remember { mutableStateOf("") }
        var lessonType by remember { mutableStateOf("") }
        var description by remember { mutableStateOf("") }
        var price by remember { mutableStateOf("") }
        var dialogError by remember { mutableStateOf<String?>(null) }

        AlertDialog(
            onDismissRequest = { showAddDialog = false },
            title = { Text("Create Offering") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = title, onValueChange = { title = it },
                        label = { Text("Title") }, singleLine = true
                    )
                    OutlinedTextField(
                        value = lessonType, onValueChange = { lessonType = it },
                        label = { Text("Lesson Type (e.g. Manual, Automatic)") }, singleLine = true
                    )
                    OutlinedTextField(
                        value = description, onValueChange = { description = it },
                        label = { Text("Description (optional)") }
                    )
                    OutlinedTextField(
                        value = price, onValueChange = { price = it },
                        label = { Text("Price (R)") }, singleLine = true
                    )
                    if (dialogError != null) {
                        Text(dialogError!!, color = MaterialTheme.colorScheme.error)
                    }
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    val priceVal = price.toDoubleOrNull()
                    if (title.isBlank() || lessonType.isBlank() || priceVal == null || priceVal <= 0) {
                        dialogError = "Fill all required fields with valid values."
                        return@TextButton
                    }
                    scope.launch {
                        try {
                            val res = apiService.createOffering(
                                CreateOfferingRequest(title, lessonType, description.ifBlank { null }, priceVal)
                            )
                            if (res.isSuccessful) {
                                showAddDialog = false
                                loadOfferings()
                            } else {
                                dialogError = "Failed to create offering."
                            }
                        } catch (_: Exception) {
                            dialogError = "Connection error."
                        }
                    }
                }) { Text("Create") }
            },
            dismissButton = {
                TextButton(onClick = { showAddDialog = false }) { Text("Cancel") }
            }
        )
    }
}
